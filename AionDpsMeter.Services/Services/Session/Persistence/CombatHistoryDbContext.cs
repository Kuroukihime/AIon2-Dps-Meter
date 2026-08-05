using Microsoft.EntityFrameworkCore;

namespace AionDpsMeter.Services.Services.Session.Persistence
{
    public sealed class CombatHistoryDbContext : DbContext
    {
        public DbSet<CombatSessionEntity> Sessions => Set<CombatSessionEntity>();

        public CombatHistoryDbContext(DbContextOptions<CombatHistoryDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CombatSessionEntity>(entity =>
            {
                entity.ToTable("CombatSessions");
                entity.HasKey(x => x.SessionId);

                entity.Property(x => x.SessionId).ValueGeneratedNever();
                entity.Property(x => x.TargetName).HasMaxLength(256);
                entity.Property(x => x.State).HasConversion<int>();
                entity.Property(x => x.PlayerStatsJson).HasDefaultValue("[]");
                entity.Property(x => x.SkillStatsByPlayerJson).HasDefaultValue("{}");
                entity.Property(x => x.BuffStatsByPlayerJson).HasDefaultValue("{}");
                entity.Property(x => x.HitsByPlayerJson).HasDefaultValue("{}");
                entity.Property(x => x.BuffEventsByPlayerJson).HasDefaultValue("{}");

                entity.HasIndex(x => x.SessionEnd);
                entity.HasIndex(x => x.TargetName);
            });
        }
    }
}
