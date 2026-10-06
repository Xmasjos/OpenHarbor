namespace OpenHarbor.Services;

public interface ILoadedPluginCatalogReader
{
    IReadOnlyList<LoadedPlugin> LoadedPlugins { get; }
}