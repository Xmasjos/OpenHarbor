using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenHarbor.Services;

namespace OpenHarbor.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin")]
public class AdminController(
    IPluginCatalogReader catalogReader,
    PluginRuntimeState runtimeState,
    IHostApplicationLifetime applicationLifetime,
    IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("antiforgery")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult GetAntiforgeryToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var records = await catalogReader.GetAllAsync(cancellationToken);
        return Ok(new
        {
            restartPending = runtimeState.RestartPending,
            selectedDashboard = runtimeState.SelectedDashboardPluginId is null ? null : new
            {
                id = runtimeState.SelectedDashboardPluginId,
                name = runtimeState.SelectedDashboardPluginName,
                routeSubpath = runtimeState.SelectedDashboardRouteSubpath
            },
            items = runtimeState.Statuses,
            totalRecords = records.Count
        });
    }

    [HttpGet("plugins")]
    public async Task<IActionResult> GetPlugins(CancellationToken cancellationToken) =>
        Ok(await catalogReader.GetAllAsync(cancellationToken));

    [HttpPost("restart")]
    [ValidateAntiForgeryToken]
    public IActionResult Restart()
    {
        applicationLifetime.StopApplication();
        return Ok(new { message = "Restart requested. The host will stop gracefully and must be relaunched by the configured supervisor." });
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}