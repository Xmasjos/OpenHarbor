using Microsoft.EntityFrameworkCore;
using OpenHarbor.Models;

namespace OpenHarbor.DAL;

public class PluginCatalogDbContext(DbContextOptions<PluginCatalogDbContext> options)
    : DbContext(options)
{
    public DbSet<PluginRecord> Plugins => Set<PluginRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PluginRecord>(entity =>
        {
            entity.Property(x => x.IsSelectedDashboardProvider)
                .HasDefaultValue(false);

            entity.HasIndex(x => x.RouteSubpath)
                .IsUnique();

            entity.HasIndex(x => x.IsSelectedDashboardProvider)
                .IsUnique()
                .HasFilter("[IsSelectedDashboardProvider] = 1");

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.RouteSubpath)
                .HasMaxLength(120)
                .IsRequired()
                .UseCollation("NOCASE");

            entity.Property(x => x.DllRelativePath)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.PublicFolderRelativePath)
                .HasMaxLength(500);
        });
    }

}
