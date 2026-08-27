namespace Dadstart.Labs.ScoreForge.Api.Data.Entities;

public sealed class UserEntity
{
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderSubject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastLoginAtUtc { get; set; }

    public ICollection<MatchEntity> OwnedMatches { get; set; } = [];
}
