using OpenHarbor.Models;

namespace OpenHarbor.Services;

public interface IPluginDataManager
{
    Task DeleteDataAsync(PluginRecord record, CancellationToken cancellationToken = default);
}