using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenHarbor.Services;

namespace OpenHarbor.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/dashboard")]
public class DashboardController(ILoadedPluginCatalogReader loadedPluginCatalog) : ControllerBase
{
    [HttpGet("applications")]
    public IActionResult GetApplications() => Ok(
        loadedPluginCatalog.LoadedPlugins
            .Where(plugin => plugin.Record.Enabled && plugin.Entry.Application is not null)
            .Select(plugin => new
            {
                id = plugin.Record.Id,
                name = plugin.Entry.Application!.Name,
                iconSvg = plugin.Entry.Application.IconSvg,
                launchMode = plugin.Entry.Application.LaunchMode,
                routeSubpath = plugin.Record.RouteSubpath.Trim('/')
            }));
}