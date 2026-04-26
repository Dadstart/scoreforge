using Dadstart.Labs.ScoreForge.Contracts;

namespace Dadstart.Labs.ScoreForge.Api.Repositories;

public interface IScoreboardRepository
{
    Task<IReadOnlyList<ScoreboardSummary>> ListSummariesAsync(CancellationToken cancellationToken = default);
}
