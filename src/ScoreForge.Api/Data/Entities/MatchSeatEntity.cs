namespace Dadstart.Labs.ScoreForge.Api.Data.Entities;

public sealed class MatchSeatEntity
{
    public Guid SeatId { get; set; }
    public Guid MatchId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? TeamId { get; set; }
    public int SortOrder { get; set; }
    public Guid? ClaimedByUserId { get; set; }

    public MatchEntity? Match { get; set; }
    public UserEntity? ClaimedByUser { get; set; }
}
