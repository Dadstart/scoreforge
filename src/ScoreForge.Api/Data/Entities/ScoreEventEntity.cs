namespace Dadstart.Labs.ScoreForge.Api.Data.Entities;

public sealed class ScoreEventEntity
{
    public Guid EventId { get; set; }
    public Guid MatchId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public Guid? ActorUserId { get; set; }
    public string ClientEventId { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }

    public MatchEntity? Match { get; set; }
    public UserEntity? Actor { get; set; }
}
