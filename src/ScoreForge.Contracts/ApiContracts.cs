using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Dadstart.Labs.ScoreForge.Contracts;

public sealed record UserProfile(
    Guid UserId,
    string DisplayName,
    string? Email,
    DateTimeOffset CreatedAtUtc);

public sealed record GameCatalogItem(
    string GameId,
    string Name,
    string Description);

public sealed record MatchSeatDto(
    Guid SeatId,
    string DisplayName,
    string? TeamId,
    int SortOrder,
    Guid? ClaimedByUserId);

public sealed record MatchEventDto(
    Guid EventId,
    string EventType,
    JsonElement Payload,
    Guid? ActorUserId,
    string ClientEventId,
    int Version,
    DateTimeOffset OccurredAtUtc);

public sealed record SeatStandingDto(
    Guid SeatId,
    string DisplayName,
    string? TeamId,
    int Score);

public sealed record TeamStandingDto(
    string TeamId,
    string DisplayName,
    int Score);

public sealed record MatchStandingsDto(
    IReadOnlyList<SeatStandingDto> Seats,
    IReadOnlyList<TeamStandingDto>? Teams,
    Guid? WinnerSeatId,
    string? WinnerTeamId,
    bool IsComplete,
    JsonElement? Details);

public sealed record MatchSummaryDto(
    Guid MatchId,
    string Name,
    string GameId,
    string Status,
    int Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record MatchSnapshotDto(
    Guid MatchId,
    string Name,
    string GameId,
    string Status,
    int Version,
    Guid OwnerUserId,
    JsonElement Options,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<MatchSeatDto> Seats,
    IReadOnlyList<MatchEventDto> Events,
    MatchStandingsDto Standings);

public sealed record CreateMatchSeatRequest(
    [property: Required, StringLength(120, MinimumLength = 1)] string DisplayName,
    [property: StringLength(64)] string? TeamId);

public sealed record CreateMatchRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    [property: Required, StringLength(64, MinimumLength = 2)] string GameId,
    JsonElement? Options,
    [property: Required, MinLength(2)] IReadOnlyList<CreateMatchSeatRequest> Seats);

public sealed record AppendMatchEventRequest(
    [property: Required, StringLength(64, MinimumLength = 2)] string EventType,
    JsonElement Payload,
    [property: Range(0, int.MaxValue)] int BaseVersion,
    [property: Required, StringLength(120, MinimumLength = 3)] string ClientEventId);

public sealed record UndoMatchEventRequest(
    Guid TargetEventId,
    [property: Range(0, int.MaxValue)] int BaseVersion,
    [property: Required, StringLength(120, MinimumLength = 3)] string ClientEventId);

public sealed record AuthProviderSummary(
    string Scheme,
    string DisplayName,
    bool Configured);

public sealed record AuthUserResponse(
    bool IsAuthenticated,
    Guid? UserId,
    string? DisplayName,
    string? Email);
