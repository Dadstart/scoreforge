using Dadstart.Labs.ScoreForge.Api.Data.Entities;
using Dadstart.Labs.ScoreForge.Games.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dadstart.Labs.ScoreForge.Api.Data;

public static class ScoreForgeDbInitializer
{
    public static async Task InitializeAsync(ScoreForgeDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.IsRelational() && (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        else
            await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        if (!await dbContext.Games.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            dbContext.Games.AddRange(
                new GameDefinitionEntity
                {
                    GameId = GameIds.Cribbage,
                    Name = "Cribbage",
                    Description = "Live pegboard scorekeeping for 2–4 players racing to 61 or 121."
                },
                new GameDefinitionEntity
                {
                    GameId = GameIds.Canasta,
                    Name = "American Canasta",
                    Description = "Partnership round scoresheet racing to 5000."
                });

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}