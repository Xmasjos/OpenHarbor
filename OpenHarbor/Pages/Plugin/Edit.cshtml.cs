using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenHarbor.Models;
using OpenHarbor.Services;

namespace OpenHarbor.Pages.Plugin;

[Authorize(Policy = "Admin")]
public class EditModel(
    IPluginCatalogReader catalogReader,
    IPluginCatalogWriter catalogWriter,
    IPluginPackageInstaller packageInstaller,
    PluginRuntimeState runtimeState) : PageModel
{
    [BindProperty]
    public PluginRecord Input { get; set; } = new();

    [BindProperty]
    public IFormFile? Package { get; set; }

    public bool IsNew => Input.Id == Guid.Empty;

    public PluginRuntimeStatus? RuntimeStatus { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            Input = new PluginRecord { Id = Guid.Empty };
            return Page();
        }

        var record = await catalogReader.GetByIdAsync(id.Value, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }

        Input = record;
        RuntimeStatus = runtimeState.Statuses.FirstOrDefault(status => status.Key == record.Id.ToString());
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Package is not null)
            ModelState.Remove("Input.DllRelativePath");

        if (!ModelState.IsValid)
            return Page();

        PluginPackage? installedPackage = null;
        var isNew = Input.Id == Guid.Empty;
        if (isNew)
            Input.Id = Guid.NewGuid();

        try
        {
            if (Package is not null)
            {
                installedPackage = await packageInstaller.InstallAsync(Input.Id, Package, cancellationToken);
                Input.DllRelativePath = installedPackage.DllRelativePath;
                Input.PublicFolderRelativePath = installedPackage.PublicFolderRelativePath;
            }

            if (isNew)
                await catalogWriter.CreateAsync(Input, cancellationToken);
            else
                await catalogWriter.UpdateAsync(Input, cancellationToken);

            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            if (installedPackage is not null)
                await packageInstaller.RemoveAsync(installedPackage, CancellationToken.None);

            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
