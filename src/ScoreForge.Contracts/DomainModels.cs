using System.ComponentModel.DataAnnotations;

namespace Dadstart.Labs.ScoreForge.Contracts;

public sealed record UserProfile(
    Guid UserId,
    [property: Required, StringLength(120, MinimumLength = 2)] string DisplayName,
    [property: StringLength(320)] string? Email,
    DateTimeOffset CreatedAtUtc);

public sealed record GameDefinition(
    Guid GameId,
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    [property: StringLength(512)] string? Description,
    DateTimeOffset CreatedAtUtc);

public sealed record ScoreboardParticipant(
    Guid ParticipantId,
    [property: Required, StringLength(120, MinimumLength = 1)] string Name,
    int SortOrder,
    DateTimeOffset JoinedAtUtc);

public sealed record ScoreEvent(
    Guid EventId,
    Guid ScoreboardId,
    Guid ParticipantId,
    int Delta,
    int Version,
    DateTimeOffset OccurredAtUtc,
    [property: StringLength(120)] string Source = "client");

public sealed record Scoreboard(
    Guid ScoreboardId,
    Guid OwnerUserId,
    Guid GameId,
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    int Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<ScoreboardParticipant> Participants,
    IReadOnlyList<ScoreEvent> Events);
