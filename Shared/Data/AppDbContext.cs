using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace Shared.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<SensorRegistry> SensorRegistry => Set<SensorRegistry>();
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<AlarmLog> AlarmLogs => Set<AlarmLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorRegistry>(entity =>
        {
            entity.HasKey(e => e.SensorId);
            entity.Property(e => e.SensorId).HasMaxLength(64);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(e => e.Quality).HasConversion<string>().HasMaxLength(16);
        });

        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SensorId).HasMaxLength(64);
            entity.Property(e => e.Quality).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.SensorId, e.Timestamp });
            entity.HasIndex(e => e.IsConsensus);
        });

        modelBuilder.Entity<AlarmLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SensorId).HasMaxLength(64);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.SensorId, e.Timestamp });
        });
    }
}
