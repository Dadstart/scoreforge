using Dadstart.Labs.ScoreForge.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dadstart.Labs.ScoreForge.Api.Data;

public sealed class ScoreForgeDbContext(DbContextOptions<ScoreForgeDbContext> options) : DbContext(options)
{
    public DbSet<ScoreboardEntity> Scoreboards => Set<ScoreboardEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScoreboardEntity>(entity =>
        {
            entity.ToTable("Scoreboards");
            entity.HasKey(x => x.ScoreboardId);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
        });
    }
}
