using Microsoft.EntityFrameworkCore;
using System.Numerics;
using Web.Entities;

namespace Web;
public sealed class AppDbContext : DbContext
{
    public DbSet<Clip> Clips { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Clip>()
            .HasMany(c => c.Detections)
            .WithOne();
        modelBuilder.Entity<Clip>()
            .Property(x => x.DateRecorded)
            .HasConversion(x => x, x => new DateTime(x.Ticks, DateTimeKind.Utc));
        modelBuilder.Entity<Clip>()
            .HasMany(x => x.Classifications)
            .WithOne();

        modelBuilder.Entity<Detection>()
            .Property(x => x.BoundingBox)
            .HasConversion(JsonValueConverter<BoundingBox>.Instance);
    }
}
