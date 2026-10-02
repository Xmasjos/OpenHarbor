using OpenHarbor.Models;

namespace OpenHarbor.Services;

public interface IPluginCatalogReader
{
    Task<List<PluginRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<PluginRecord>> GetEnabledAsync(CancellationToken cancellationToken = default);

    Task<PluginRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}