using System.Text.Json;
using Dadstart.Labs.ScoreForge.Api.Data;
using Dadstart.Labs.ScoreForge.Api.Data.Entities;
using Dadstart.Labs.ScoreForge.Api.Hubs;
using Dadstart.Labs.ScoreForge.Contracts;
using Dadstart.Labs.ScoreForge.Games.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Dadstart.Labs.ScoreForge.Api.Services;

public interface IMatchService
{
    Task<IReadOnlyList<GameCatalogItem>> GetGamesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MatchSummaryDto>> ListMatchesAsync(Guid userId, CancellationToken cancellationToken);
    Task<MatchSnapshotDto> CreateMatchAsync(Guid userId, CreateMatchRequest request, CancellationToken cancellationToken);
    Task<MatchSnapshotDto?> GetMatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken);
    Task<MatchSnapshotDto?> ClaimSeatAsync(Guid matchId, Guid seatId, Guid userId, CancellationToken cancellationToken);
    Task<MatchAppendResult> AppendEventAsync(Guid matchId, Guid userId, AppendMatchEventRequest request, CancellationToken cancellationToken);
    Task<MatchAppendResult> UndoEventAsync(Guid matchId, Guid userId, UndoMatchEventRequest request, CancellationToken cancellationToken);
    Task<bool> CanAccessMatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken);
}

public sealed record MatchAppendResult(bool Success, int? StatusCode, MatchSnapshotDto? Snapshot, string? Error);

public sealed class MatchService(
    ScoreForgeDbContext dbContext,
    IEnumerable<IGameEngine> engines,
    IHubContext<MatchHub> hubContext) : IMatchService
{
    private readonly Dictionary<string, IGameEngine> _engines = engines.ToDictionary(e => e.GameId, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<GameCatalogItem>> GetGamesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Games
            .OrderBy(g => g.Name)
            .Select(g => new GameCatalogItem(g.GameId, g.Name, g.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MatchSummaryDto>> ListMatchesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Matches
            .AsNoTracking()
            .Where(m => m.OwnerUserId == userId || m.Seats.Any(s => s.ClaimedByUserId == userId))
            .OrderByDescending(m => m.UpdatedAtUtc)
            .Select(m => new MatchSummaryDto(m.MatchId, m.Name, m.GameId, m.Status, m.Version, m.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<MatchSnapshotDto> CreateMatchAsync(Guid userId, CreateMatchRequest request, CancellationToken cancellationToken)
    {
        if (!_engines.TryGetValue(request.GameId, out var engine))
            throw new InvalidOperationException($"Unknown game '{request.GameId}'.");

        var gameExists = await dbContext.Games.AnyAsync(g => g.GameId == request.GameId, cancellationToken).ConfigureAwait(false);
        if (!gameExists)
            throw new InvalidOperationException($"Game '{request.GameId}' is not registered.");

        var optionsElement = request.Options ?? JsonSerializer.SerializeToElement(new { });
        var seats = request.Seats
            .Select((seat, index) => new SeatInfo(Guid.NewGuid(), seat.DisplayName.Trim(), seat.TeamId, index))
            .ToArray();

        var initial = engine.CreateInitialState(new MatchOptions(optionsElement), seats);
        var now = DateTimeOffset.UtcNow;
        var match = new MatchEntity
        {
            MatchId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            GameId = request.GameId,
            OwnerUserId = userId,
            Status = MatchStatuses.Active,
            Version = 0,
            OptionsJson = optionsElement.GetRawText(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Seats = seats.Select(s => new MatchSeatEntity
            {
                SeatId = s.SeatId,
                DisplayName = s.DisplayName,
                TeamId = s.TeamId,
                SortOrder = s.SortOrder,
                ClaimedByUserId = s.SortOrder == 0 ? userId : null
            }).ToList()
        };

        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await BuildSnapshotAsync(match.MatchId, cancellationToken).ConfigureAwait(false)
               ?? throw new InvalidOperationException("Failed to load created match.");
    }

    public async Task<bool> CanAccessMatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
    {
        _ = userId;
        return await dbContext.Matches
            .AsNoTracking()
            .AnyAsync(m => m.MatchId == matchId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<MatchSnapshotDto?> GetMatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
    {
        _ = userId;
        return await BuildSnapshotAsync(matchId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MatchSnapshotDto?> ClaimSeatAsync(Guid matchId, Guid seatId, Guid userId, CancellationToken cancellationToken)
    {
        var match = await dbContext.Matches
            .Include(m => m.Seats)
            .FirstOrDefaultAsync(m => m.MatchId == matchId, cancellationToken)
            .ConfigureAwait(false);

        if (match is null)
            return null;

        var seat = match.Seats.FirstOrDefault(s => s.SeatId == seatId);
        if (seat is null)
            return null;

        if (seat.ClaimedByUserId is not null && seat.ClaimedByUserId != userId)
            throw new InvalidOperationException("Seat is already claimed.");

        foreach (var other in match.Seats.Where(s => s.ClaimedByUserId == userId && s.SeatId != seatId))
            other.ClaimedByUserId = null;

        seat.ClaimedByUserId = userId;
        match.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var snapshot = await BuildSnapshotAsync(matchId, cancellationToken).ConfigureAwait(false);
        if (snapshot is not null)
            await PublishAsync(snapshot, cancellationToken).ConfigureAwait(false);

        return snapshot;
    }

    public async Task<MatchAppendResult> AppendEventAsync(
        Guid matchId,
        Guid userId,
        AppendMatchEventRequest request,
        CancellationToken cancellationToken)
    {
        if (string.Equals(request.EventType, MatchEventTypes.Undo, StringComparison.OrdinalIgnoreCase))
            return new MatchAppendResult(false, StatusCodes.Status400BadRequest, null, "Use the undo endpoint for undo events.");

        return await AppendInternalAsync(matchId, userId, request.EventType, request.Payload, request.BaseVersion, request.ClientEventId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<MatchAppendResult> UndoEventAsync(
        Guid matchId,
        Guid userId,
        UndoMatchEventRequest request,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToElement(new { targetEventId = request.TargetEventId });
        return await AppendInternalAsync(matchId, userId, MatchEventTypes.Undo, payload, request.BaseVersion, request.ClientEventId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<MatchAppendResult> AppendInternalAsync(
        Guid matchId,
        Guid userId,
        string eventType,
        JsonElement payload,
        int baseVersion,
        string clientEventId,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessMatchAsync(matchId, userId, cancellationToken).ConfigureAwait(false))
            return new MatchAppendResult(false, StatusCodes.Status404NotFound, null, "Match not found.");

        var existing = await dbContext.ScoreEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.MatchId == matchId && e.ClientEventId == clientEventId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var existingSnapshot = await BuildSnapshotAsync(matchId, cancellationToken).ConfigureAwait(false);
            return new MatchAppendResult(true, StatusCodes.Status200OK, existingSnapshot, null);
        }

        var match = await dbContext.Matches
            .Include(m => m.Seats)
            .FirstOrDefaultAsync(m => m.MatchId == matchId, cancellationToken)
            .ConfigureAwait(false);

        if (match is null)
            return new MatchAppendResult(false, StatusCodes.Status404NotFound, null, "Match not found.");

        if (baseVersion != match.Version)
        {
            var conflict = await BuildSnapshotAsync(matchId, cancellationToken).ConfigureAwait(false);
            return new MatchAppendResult(false, StatusCodes.Status409Conflict, conflict, "Version conflict.");
        }

        if (!_engines.TryGetValue(match.GameId, out var engine))
            return new MatchAppendResult(false, StatusCodes.Status500InternalServerError, null, "Game engine missing.");

        var priorEvents = await dbContext.ScoreEvents
            .AsNoTracking()
            .Where(e => e.MatchId == matchId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var eventId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var newEvent = new ScoreEventEntity
        {
            EventId = eventId,
            MatchId = matchId,
            EventType = eventType,
            PayloadJson = payload.GetRawText(),
            ActorUserId = userId,
            ClientEventId = clientEventId,
            Version = match.Version + 1,
            OccurredAtUtc = occurredAt
        };

        var eventsForValidation = priorEvents.Append(newEvent).ToList();
        MatchStandings standings;
        try
        {
            var state = RebuildState(engine, match, eventsForValidation);
            standings = engine.GetStandings(state);
        }
        catch (GameEngineException ex)
        {
            return new MatchAppendResult(false, StatusCodes.Status400BadRequest, null, ex.Message);
        }

        dbContext.ScoreEvents.Add(newEvent);
        match.Version = newEvent.Version;
        match.UpdatedAtUtc = occurredAt;
        match.Status = standings.IsComplete ? MatchStatuses.Completed : MatchStatuses.Active;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var snapshot = await BuildSnapshotAsync(matchId, cancellationToken).ConfigureAwait(false);
        if (snapshot is not null)
        {
            try
            {
                await PublishAsync(snapshot, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Live fan-out should not fail the write path.
            }
        }

        return new MatchAppendResult(true, StatusCodes.Status200OK, snapshot, null);
    }

    private async Task<MatchSnapshotDto?> BuildSnapshotAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var match = await dbContext.Matches
            .AsNoTracking()
            .Include(m => m.Seats)
            .Include(m => m.Events)
            .FirstOrDefaultAsync(m => m.MatchId == matchId, cancellationToken)
            .ConfigureAwait(false);

        if (match is null || !_engines.TryGetValue(match.GameId, out var engine))
            return null;

        var orderedEvents = match.Events.OrderBy(e => e.Version).ToList();
        var state = RebuildState(engine, match, orderedEvents);
        var standings = engine.GetStandings(state);
        var options = JsonDocument.Parse(match.OptionsJson).RootElement.Clone();

        return new MatchSnapshotDto(
            match.MatchId,
            match.Name,
            match.GameId,
            match.Status,
            match.Version,
            match.OwnerUserId,
            options,
            match.CreatedAtUtc,
            match.UpdatedAtUtc,
            match.Seats
                .OrderBy(s => s.SortOrder)
                .Select(s => new MatchSeatDto(s.SeatId, s.DisplayName, s.TeamId, s.SortOrder, s.ClaimedByUserId))
                .ToArray(),
            orderedEvents
                .Select(e => new MatchEventDto(
                    e.EventId,
                    e.EventType,
                    JsonDocument.Parse(e.PayloadJson).RootElement.Clone(),
                    e.ActorUserId,
                    e.ClientEventId,
                    e.Version,
                    e.OccurredAtUtc))
                .ToArray(),
            new MatchStandingsDto(
                standings.Seats.Select(s => new SeatStandingDto(s.SeatId, s.DisplayName, s.TeamId, s.Score)).ToArray(),
                standings.Teams?.Select(t => new TeamStandingDto(t.TeamId, t.DisplayName, t.Score)).ToArray(),
                standings.WinnerSeatId,
                standings.WinnerTeamId,
                standings.IsComplete,
                standings.Details));
    }

    private static GameState RebuildState(IGameEngine engine, MatchEntity match, IReadOnlyList<ScoreEventEntity> events)
    {
        var seats = match.Seats
            .OrderBy(s => s.SortOrder)
            .Select(s => new SeatInfo(s.SeatId, s.DisplayName, s.TeamId, s.SortOrder))
            .ToArray();
        var options = JsonDocument.Parse(match.OptionsJson).RootElement.Clone();
        var state = engine.CreateInitialState(new MatchOptions(options), seats);

        var undone = new HashSet<Guid>();
        foreach (var evt in events.Where(e => e.EventType == MatchEventTypes.Undo))
        {
            using var doc = JsonDocument.Parse(evt.PayloadJson);
            if (doc.RootElement.TryGetProperty("targetEventId", out var target) &&
                target.TryGetGuid(out var targetId))
                undone.Add(targetId);
        }

        foreach (var evt in events)
        {
            if (evt.EventType == MatchEventTypes.Undo || undone.Contains(evt.EventId))
                continue;

            using var doc = JsonDocument.Parse(evt.PayloadJson);
            state = engine.Apply(
                state,
                new ScoreEventInput(evt.EventId, evt.EventType, doc.RootElement.Clone(), evt.ActorUserId, evt.OccurredAtUtc));
        }

        return state;
    }

    private Task PublishAsync(MatchSnapshotDto snapshot, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(MatchHub.GroupName(snapshot.MatchId))
            .SendAsync("MatchUpdated", snapshot, cancellationToken);
}
