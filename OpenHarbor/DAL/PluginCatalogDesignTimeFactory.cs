using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenHarbor.DAL;

public class PluginCatalogDesignTimeFactory : IDesignTimeDbContextFactory<PluginCatalogDbContext>
{
    public PluginCatalogDbContext CreateDbContext(string[] args)
    {
        var connectionStringArgumentIndex = Array.IndexOf(args, "--connection-string");
        if (connectionStringArgumentIndex < 0 ||
            connectionStringArgumentIndex + 1 >= args.Length ||
            string.IsNullOrWhiteSpace(args[connectionStringArgumentIndex + 1]))
        {
            throw new ArgumentException(
                "Pass the catalog connection string with '-- --connection-string <connection-string>'.",
                nameof(args));
        }

        var options = new DbContextOptionsBuilder<PluginCatalogDbContext>()
            .UseSqlite(args[connectionStringArgumentIndex + 1])
            .Options;

        return new PluginCatalogDbContext(options);
    }
}