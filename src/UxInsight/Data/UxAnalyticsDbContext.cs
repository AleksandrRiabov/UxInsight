using Microsoft.EntityFrameworkCore;
using UxInsight.Models;

namespace UxInsight.Data;

public class UxAnalyticsDbContext : DbContext
{
    public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            var dataDir = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString()
                          ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "umbraco", "Data");
            var dbPath = Path.Combine(dataDir, "UxAnalytics.sqlite.db");
            options.UseSqlite($"Data Source={dbPath};Cache=Shared");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateTimeOffsetConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter();

        modelBuilder.Entity<TrackingEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SessionId);
            e.HasIndex(x => x.EventType);
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.PageUrl);
            e.Property(x => x.Timestamp).HasConversion(dateTimeOffsetConverter);
        });

        modelBuilder.Entity<AnalysisResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.CreatedAt).HasConversion(dateTimeOffsetConverter);
            e.Property(x => x.AnalysisPeriodStart).HasConversion(dateTimeOffsetConverter);
            e.Property(x => x.AnalysisPeriodEnd).HasConversion(dateTimeOffsetConverter);
        });
    }
}
