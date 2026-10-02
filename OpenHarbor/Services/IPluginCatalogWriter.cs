using OpenHarbor.Models;

namespace OpenHarbor.Services;

public interface IPluginCatalogWriter
{
    Task<PluginRecord> CreateAsync(PluginRecord record, CancellationToken cancellationToken = default);

    Task<PluginRecord> UpdateAsync(PluginRecord record, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}