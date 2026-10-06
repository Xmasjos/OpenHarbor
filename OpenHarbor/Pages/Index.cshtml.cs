using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenHarbor.Models;
using OpenHarbor.Services;

namespace OpenHarbor.Pages;

[Authorize(Policy = "Admin")]
public class IndexModel(
    IPluginCatalogReader catalogReader,
    IPluginCatalogWriter catalogWriter,
    PluginRuntimeState runtimeState) : PageModel
{
    public IList<PluginRecord> Plugins { get; private set; } = [];

    public bool RestartPending => runtimeState.RestartPending;

    public PluginRuntimeStatus? GetRuntimeStatus(Guid pluginId) =>
        runtimeState.Statuses.FirstOrDefault(status => status.Key == pluginId.ToString());

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Plugins = await catalogReader.GetAllAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await catalogWriter.DeleteAsync(id, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSelectDashboardProviderAsync(Guid id, CancellationToken cancellationToken)
    {
        await catalogWriter.SelectDashboardProviderAsync(id, cancellationToken);
        return RedirectToPage();
    }
}
