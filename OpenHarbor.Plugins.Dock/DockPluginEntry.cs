using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenHarbor.PluginContracts;

namespace OpenHarbor.Plugins.Dock;

public class DockPluginEntry : IPluginEntry
{
    public string Name => "Dock";

    public string Version => "1.0.0";

    public PluginApplicationDescriptor? Application => null;

    public PluginDashboardCapability? Dashboard => new("Dock");

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}