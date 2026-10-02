using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenHarbor.Models;
using OpenHarbor.Services;

namespace OpenHarbor.Pages.Plugin;

[Authorize(Policy = "Admin")]
public class EditModel(IPluginCatalogReader catalogReader, IPluginCatalogWriter catalogWriter) : PageModel
{
    [BindProperty]
    public PluginRecord Input { get; set; } = new();

    public bool IsNew => Input.Id == Guid.Empty;

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            Input = new PluginRecord();
            return Page();
        }

        var record = await catalogReader.GetByIdAsync(id.Value, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }

        Input = record;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            if (Input.Id == Guid.Empty)
            {
                await catalogWriter.CreateAsync(Input, cancellationToken);
            }
            else
            {
                await catalogWriter.UpdateAsync(Input, cancellationToken);
            }

            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
