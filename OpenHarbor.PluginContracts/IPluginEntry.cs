using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace OpenHarbor.PluginContracts;

public interface IPluginEntry
{
    string Name { get; }

    string Version { get; }

    PluginApplicationDescriptor? Application { get; }

    PluginDashboardCapability? Dashboard { get; }

    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
