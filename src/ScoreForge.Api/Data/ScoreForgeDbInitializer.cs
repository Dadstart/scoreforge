using Dadstart.Labs.ScoreForge.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dadstart.Labs.ScoreForge.Api.Data;

public static class ScoreForgeDbInitializer
{
    private static readonly Guid StarterScoreboardId = Guid.Parse("A09B8E7B-7428-4577-B1DF-4A7E9AA16E59");

    public static async Task InitializeAsync(ScoreForgeDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var hasAny = await dbContext.Scoreboards.AnyAsync(cancellationToken).ConfigureAwait(false);
        if (hasAny)
            return;

        dbContext.Scoreboards.Add(new ScoreboardEntity
        {
            ScoreboardId = StarterScoreboardId,
            Name = "Starter Match",
            Version = 0,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
