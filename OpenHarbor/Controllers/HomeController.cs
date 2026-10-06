using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenHarbor.Options;
using OpenHarbor.Services;

namespace OpenHarbor.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("")]
public class HomeController(
    ILoadedPluginCatalogReader loadedPluginCatalog,
    IOptions<PluginServerOptions> pluginOptions,
    PluginRuntimeState runtimeState,
    IPluginCatalogReader catalogReader) : ControllerBase
{
    private readonly PluginServerOptions _pluginOptions = pluginOptions.Value;

    [HttpGet("_host/plugin-ui.js")]
    public IActionResult GetPluginUi()
    {
        var selectedPlugin = loadedPluginCatalog.LoadedPlugins.FirstOrDefault(
            plugin => plugin.Record.Id == runtimeState.SelectedDashboardPluginId);
        if (selectedPlugin?.Entry.Dashboard is null || string.IsNullOrWhiteSpace(selectedPlugin.Record.PublicFolderRelativePath))
            return NotFound();

        var runtimePath = Path.GetFullPath(Path.Combine(
            _pluginOptions.ManagedPluginRoot,
            selectedPlugin.Record.PublicFolderRelativePath.Replace('/', Path.DirectorySeparatorChar),
            "plugin-ui.js"));
        return System.IO.File.Exists(runtimePath)
            ? PhysicalFile(runtimePath, "text/javascript; charset=utf-8")
            : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        if (runtimeState.SelectedDashboardPluginId is not Guid selectedPluginId)
            return Content("<!doctype html><html lang=\"en\"><title>Dashboard unavailable</title><h1>No dashboard provider is selected.</h1><a href=\"/admin/plugins\">Open plugin catalog</a></html>", "text/html");

        var record = await catalogReader.GetByIdAsync(selectedPluginId, cancellationToken);
        var loadedPlugin = loadedPluginCatalog.LoadedPlugins.FirstOrDefault(plugin => plugin.Record.Id == selectedPluginId);
        if (record is null || !record.Enabled || loadedPlugin?.Entry.Dashboard is null || string.IsNullOrWhiteSpace(record.PublicFolderRelativePath))
            return Content("<!doctype html><html lang=\"en\"><title>Dashboard unavailable</title><h1>The selected dashboard is unavailable.</h1><a href=\"/admin/plugins\">Open plugin catalog</a></html>", "text/html");

        var publicDirectory = Path.GetFullPath(Path.Combine(
            _pluginOptions.ManagedPluginRoot,
            record.PublicFolderRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        var dashboardFile = Path.Combine(publicDirectory, "index.html");
        if (!System.IO.File.Exists(dashboardFile))
            return Content("<!doctype html><html lang=\"en\"><title>Dashboard unavailable</title><h1>The selected dashboard has no public index.html.</h1><a href=\"/admin/plugins\">Open plugin catalog</a></html>", "text/html");

        var dashboardHtml = await System.IO.File.ReadAllTextAsync(dashboardFile, cancellationToken);
        var baseElement = $"<base href=\"/plugins/{record.RouteSubpath.Trim('/')}/\">";
        var dashboardIconSvg = loadedPlugin.Entry.Application?.IconSvg;
        var faviconHref = string.IsNullOrWhiteSpace(dashboardIconSvg)
            ? "/icon.svg"
            : $"data:image/svg+xml,{Uri.EscapeDataString(dashboardIconSvg)}";
        var faviconElement = $"<link rel=\"icon\" type=\"image/svg+xml\" href=\"{faviconHref}\">";
        dashboardHtml = dashboardHtml.Replace("<head>", $"<head>{baseElement}{faviconElement}", StringComparison.OrdinalIgnoreCase);
        return Content(dashboardHtml, "text/html; charset=utf-8");
    }
}