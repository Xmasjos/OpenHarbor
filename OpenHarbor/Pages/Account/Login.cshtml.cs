using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OpenHarbor.Pages.Account;

[AllowAnonymous]
public class LoginModel(IConfiguration configuration) : PageModel
{
    [BindProperty]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var expectedUser = Environment.GetEnvironmentVariable("PLUGIN_ADMIN_USERNAME")
            ?? configuration["PluginServer:AdminUserName"]
            ?? configuration["PluginServer:AdminUsername"];

        var expectedPassword = Environment.GetEnvironmentVariable("PLUGIN_ADMIN_PASSWORD")
            ?? configuration["PluginServer:AdminPassword"];

        if (string.IsNullOrWhiteSpace(expectedUser) || string.IsNullOrWhiteSpace(expectedPassword))
        {
            ModelState.AddModelError(string.Empty, "Administrative credentials are not configured.");
            return Page();
        }

        if (!string.Equals(UserName, expectedUser, StringComparison.Ordinal) ||
            !string.Equals(Password, expectedPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, expectedUser),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
        });

        return RedirectToPage("/Index");
    }
}
