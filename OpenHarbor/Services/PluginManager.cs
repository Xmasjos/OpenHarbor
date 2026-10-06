using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using OpenHarbor.PluginContracts;
using OpenHarbor.Models;
using OpenHarbor.Options;

namespace OpenHarbor.Services;

public class PluginManager : ILoadedPluginCatalogReader, IPluginDataManager
{
    private readonly List<LoadedPlugin> _loadedPlugins = [];
    private string? _managedPluginRoot;
    private IConfiguration? _configuration;

    public IReadOnlyList<LoadedPlugin> LoadedPlugins => _loadedPlugins;

    public void RegisterEnabledPlugins(
        IServiceCollection services,
        IConfiguration configuration,
        PluginServerOptions options,
        IReadOnlyList<PluginRecord> enabledPlugins)
    {
        _loadedPlugins.Clear();

        var rootDirectory = Path.GetFullPath(options.ManagedPluginRoot);
        _managedPluginRoot = rootDirectory;
        _configuration = configuration;
        Directory.CreateDirectory(rootDirectory);

        foreach (var record in enabledPlugins)
        {
            var dllPath = Path.GetFullPath(Path.Combine(rootDirectory, record.DllRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(dllPath))
            {
                _loadedPlugins.Add(new LoadedPlugin(record, new BrokenPlugin(record.Name, $"The assembly file was not found: {record.DllRelativePath}"), dllPath));
                continue;
            }

            var pluginDirectory = Path.GetDirectoryName(dllPath) ?? rootDirectory;
            var loadContext = new PluginAssemblyLoadContext(pluginDirectory);
            try
            {
                var assembly = loadContext.LoadFromAssemblyPath(dllPath);
                var entryType = assembly
                    .GetTypes()
                    .FirstOrDefault(type => typeof(IPluginEntry).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass);

                if (entryType is null)
                {
                    _loadedPlugins.Add(new LoadedPlugin(record, new BrokenPlugin(record.Name, "The plugin assembly does not implement IPluginEntry."), dllPath));
                    continue;
                }

                if (Activator.CreateInstance(entryType) is not IPluginEntry pluginInstance)
                {
                    _loadedPlugins.Add(new LoadedPlugin(record, new BrokenPlugin(record.Name, "The plugin entrypoint could not be created."), dllPath));
                    continue;
                }

                pluginInstance.ConfigureServices(services, configuration);
                _loadedPlugins.Add(new LoadedPlugin(record, pluginInstance, dllPath));
            }
            catch (Exception ex)
            {
                _loadedPlugins.Add(new LoadedPlugin(record, new BrokenPlugin(record.Name, ex.Message), dllPath));
            }
        }
    }

    public void ConfigurePublicAssets(WebApplication app, PluginServerOptions options)
    {
        var rootDirectory = Path.GetFullPath(options.ManagedPluginRoot);

        foreach (var plugin in _loadedPlugins)
        {
            if (string.IsNullOrWhiteSpace(plugin.Record.PublicFolderRelativePath))
            {
                continue;
            }

            var publicDirectory = Path.GetFullPath(Path.Combine(rootDirectory, plugin.Record.PublicFolderRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!Directory.Exists(publicDirectory))
            {
                continue;
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(publicDirectory),
                RequestPath = $"/plugins/{plugin.Record.RouteSubpath.Trim('/')}"
            });
        }
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        foreach (var loaded in _loadedPlugins)
        {
            var group = endpoints.MapGroup($"/plugins/{loaded.Record.RouteSubpath.Trim('/')}");
            loaded.Entry.MapEndpoints(group);
        }
    }

    public async Task DeleteDataAsync(PluginRecord record, CancellationToken cancellationToken = default)
    {
        var loaded = _loadedPlugins.FirstOrDefault(plugin => plugin.Record.Id == record.Id);
        if (loaded?.Entry is IPluginDataCleanup loadedCleanup)
        {
            await loadedCleanup.DeleteDataAsync(cancellationToken);
            return;
        }

        if (_managedPluginRoot is null || _configuration is null)
            return;

        var dllPath = Path.GetFullPath(Path.Combine(
            _managedPluginRoot,
            record.DllRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(dllPath))
            return;

        var loadContext = new PluginAssemblyLoadContext(Path.GetDirectoryName(dllPath) ?? _managedPluginRoot);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(dllPath);
            var entryType = assembly
                .GetTypes()
                .FirstOrDefault(type =>
                    typeof(IPluginEntry).IsAssignableFrom(type)
                    && typeof(IPluginDataCleanup).IsAssignableFrom(type)
                    && !type.IsAbstract
                    && type.IsClass);
            if (entryType is null || Activator.CreateInstance(entryType) is not IPluginEntry pluginEntry)
                return;

            pluginEntry.ConfigureServices(new ServiceCollection(), _configuration);
            if (pluginEntry is IPluginDataCleanup cleanup)
                await cleanup.DeleteDataAsync(cancellationToken);
        }
        finally
        {
            loadContext.Unload();
        }
    }
}

public record LoadedPlugin(PluginRecord Record, IPluginEntry Entry, string AssemblyPath);

public class BrokenPlugin(string name, string diagnostic) : IPluginEntry
{
    public string Name => name;

    public string Version => "unknown";

    public PluginApplicationDescriptor? Application => null;

    public PluginDashboardCapability? Dashboard => null;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/diagnostic", () => Results.Problem(diagnostic));
    }
}

public class PluginAssemblyLoadContext(string rootDirectory) : AssemblyLoadContext(isCollectible: true)
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (string.Equals(assemblyName.Name, typeof(IPluginEntry).Assembly.GetName().Name, StringComparison.Ordinal))
        {
            return typeof(IPluginEntry).Assembly;
        }

        var candidate = Path.Combine(rootDirectory, assemblyName.Name + ".dll");
        if (File.Exists(candidate))
        {
            return LoadFromAssemblyPath(candidate);
        }

        return null;
    }
}
