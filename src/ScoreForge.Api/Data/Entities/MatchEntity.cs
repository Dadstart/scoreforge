namespace Dadstart.Labs.ScoreForge.Api.Data.Entities;

public sealed class MatchEntity
{
    public Guid MatchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public string Status { get; set; } = "active";
    public int Version { get; set; }
    public string OptionsJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public UserEntity? Owner { get; set; }
    public GameDefinitionEntity? Game { get; set; }
    public ICollection<MatchSeatEntity> Seats { get; set; } = [];
    public ICollection<ScoreEventEntity> Events { get; set; } = [];
}
