namespace Dadstart.Labs.ScoreForge.Api.Data.Entities;

public sealed class ScoreboardEntity
{
    public Guid ScoreboardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
