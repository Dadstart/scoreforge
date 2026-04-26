using Dadstart.Labs.ScoreForge.Contracts;

namespace Dadstart.Labs.ScoreForge.Api.Services;

public interface IScoreboardService
{
    Task<IReadOnlyList<ScoreboardSummary>> GetFoundationScoreboardsAsync(CancellationToken cancellationToken = default);
}
