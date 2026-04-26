using System.ComponentModel.DataAnnotations;

namespace Dadstart.Labs.ScoreForge.Contracts;

public sealed record CreateScoreboardRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    Guid GameId);

public sealed record AddParticipantRequest(
    [property: Required, StringLength(120, MinimumLength = 1)] string Name);

public sealed record AppendScoreEventRequest(
    Guid ParticipantId,
    [property: Range(-10_000, 10_000)] int Delta,
    [property: Range(0, int.MaxValue)] int BaseVersion,
    [property: Required, StringLength(120, MinimumLength = 3)] string ClientEventId);

public sealed record ScoreboardSummary(
    Guid ScoreboardId,
    string Name,
    int Version,
    DateTimeOffset UpdatedAtUtc);
