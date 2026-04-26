using Dadstart.Labs.ScoreForge.Api.Data;
using Dadstart.Labs.ScoreForge.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Dadstart.Labs.ScoreForge.Api.Repositories;

public sealed class EfScoreboardRepository(ScoreForgeDbContext dbContext) : IScoreboardRepository
{
    public async Task<IReadOnlyList<ScoreboardSummary>> ListSummariesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Scoreboards
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => new ScoreboardSummary(
                x.ScoreboardId,
                x.Name,
                x.Version,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
