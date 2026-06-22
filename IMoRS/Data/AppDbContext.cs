using IMoRS.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMoRS.Data;

public class AppDbContext : DbContext
{
    public DbSet<MarkerEntity> Markers => Set<MarkerEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath.GetPath()}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarkerEntity>(entity =>
        {
            entity.ToTable("Marker");
            entity.HasKey(a => a.Id);
        });
    }
}