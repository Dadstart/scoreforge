using Dadstart.Labs.ScoreForge.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dadstart.Labs.ScoreForge.Api.Data;

public sealed class ScoreForgeDbContext(DbContextOptions<ScoreForgeDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<GameDefinitionEntity> Games => Set<GameDefinitionEntity>();
    public DbSet<MatchEntity> Matches => Set<MatchEntity>();
    public DbSet<MatchSeatEntity> MatchSeats => Set<MatchSeatEntity>();
    public DbSet<ScoreEventEntity> ScoreEvents => Set<ScoreEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.Provider).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ProviderSubject).HasMaxLength(256).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.HasIndex(x => new { x.Provider, x.ProviderSubject }).IsUnique();
        });

        modelBuilder.Entity<GameDefinitionEntity>(entity =>
        {
            entity.ToTable("Games");
            entity.HasKey(x => x.GameId);
            entity.Property(x => x.GameId).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(512).IsRequired();
        });

        modelBuilder.Entity<MatchEntity>(entity =>
        {
            entity.ToTable("Matches");
            entity.HasKey(x => x.MatchId);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.GameId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.OptionsJson).IsRequired();
            entity.HasOne(x => x.Owner).WithMany(x => x.OwnedMatches).HasForeignKey(x => x.OwnerUserId);
            entity.HasOne(x => x.Game).WithMany().HasForeignKey(x => x.GameId);
            entity.HasIndex(x => x.OwnerUserId);
            entity.HasIndex(x => x.UpdatedAtUtc);
        });

        modelBuilder.Entity<MatchSeatEntity>(entity =>
        {
            entity.ToTable("MatchSeats");
            entity.HasKey(x => x.SeatId);
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.TeamId).HasMaxLength(64);
            entity.HasOne(x => x.Match).WithMany(x => x.Seats).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ClaimedByUser).WithMany().HasForeignKey(x => x.ClaimedByUserId);
            entity.HasIndex(x => x.MatchId);
        });

        modelBuilder.Entity<ScoreEventEntity>(entity =>
        {
            entity.ToTable("ScoreEvents");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.ClientEventId).HasMaxLength(120).IsRequired();
            entity.HasOne(x => x.Match).WithMany(x => x.Events).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorUserId);
            entity.HasIndex(x => new { x.MatchId, x.ClientEventId }).IsUnique();
            entity.HasIndex(x => new { x.MatchId, x.Version }).IsUnique();
        });
    }
}
