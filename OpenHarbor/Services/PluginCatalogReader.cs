using Microsoft.EntityFrameworkCore;
using OpenHarbor.DAL;
using OpenHarbor.Models;

namespace OpenHarbor.Services;

public class PluginCatalogReader(PluginCatalogDbContext dbContext) : IPluginCatalogReader
{
    public Task<List<PluginRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        dbContext.Plugins
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<List<PluginRecord>> GetEnabledAsync(CancellationToken cancellationToken = default) =>
        dbContext.Plugins
            .AsNoTracking()
            .Where(x => x.Enabled)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<PluginRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Plugins
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}