namespace OpenHarbor.PluginContracts;

public interface IPluginDataCleanup
{
    Task DeleteDataAsync(CancellationToken cancellationToken = default);
}