using System.Text.Json;

namespace Dadstart.Labs.ScoreForge.Games.Abstractions;

public interface IGameEngine
{
    string GameId { get; }

    GameState CreateInitialState(MatchOptions options, IReadOnlyList<SeatInfo> seats);

    GameState Apply(GameState state, ScoreEventInput input);

    MatchStandings GetStandings(GameState state);

    bool IsComplete(GameState state);
}

public sealed record SeatInfo(Guid SeatId, string DisplayName, string? TeamId, int SortOrder);

public sealed record MatchOptions(JsonElement? Values);

public sealed record ScoreEventInput(
    Guid EventId,
    string EventType,
    JsonElement Payload,
    Guid? ActorUserId,
    DateTimeOffset OccurredAtUtc);

public sealed record GameState(
    string GameId,
    IReadOnlyList<SeatInfo> Seats,
    JsonElement Options,
    JsonElement Data);

public sealed record SeatStanding(Guid SeatId, string DisplayName, string? TeamId, int Score);

public sealed record MatchStandings(
    IReadOnlyList<SeatStanding> Seats,
    IReadOnlyList<TeamStanding>? Teams,
    Guid? WinnerSeatId,
    string? WinnerTeamId,
    bool IsComplete,
    JsonElement? Details);

public sealed record TeamStanding(string TeamId, string DisplayName, int Score);
