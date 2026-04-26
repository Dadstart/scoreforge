using Dadstart.Labs.ScoreForge.Api.Repositories;
using Dadstart.Labs.ScoreForge.Contracts;

namespace Dadstart.Labs.ScoreForge.Api.Services;

public sealed class ScoreboardService(IScoreboardRepository scoreboardRepository) : IScoreboardService
{
    public async Task<IReadOnlyList<ScoreboardSummary>> GetFoundationScoreboardsAsync(CancellationToken cancellationToken = default)
    {
        return await scoreboardRepository.ListSummariesAsync(cancellationToken).ConfigureAwait(false);
    }
}
