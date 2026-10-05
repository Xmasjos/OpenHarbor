using Microsoft.Extensions.Options;
using OpenHarbor.Options;

namespace OpenHarbor.Services;

public class PluginRuntimeStateInitializer(
    IServiceScopeFactory scopeFactory,
    PluginRuntimeState runtimeState,
    IOptions<PluginServerOptions> options,
    ILogger<PluginRuntimeStateInitializer> logger) : BackgroundService
{
    private readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(
        Math.Max(1, options.Value.RuntimeStateInitializationRetryIntervalSeconds));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var catalogReader = scope.ServiceProvider.GetRequiredService<IPluginCatalogReader>();
                var enabledPlugins = await catalogReader.GetEnabledAsync(stoppingToken);
                var selectedDashboard = enabledPlugins.FirstOrDefault(plugin => plugin.IsSelectedDashboardProvider)
                    ?? enabledPlugins
                        .Where(plugin => plugin.IsDashboardProvider)
                        .OrderByDescending(plugin => plugin.CreatedUtc)
                        .ThenBy(plugin => plugin.Id)
                        .FirstOrDefault();
                var statuses = enabledPlugins
                    .Select(plugin => new PluginRuntimeStatus(plugin.Id.ToString(), plugin.Name, "Loaded", null, "unknown"))
                    .ToList();

                runtimeState.ReplaceStatuses(statuses);
                runtimeState.SetSelectedDashboard(selectedDashboard);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Plugin runtime state initialization failed. Retrying in {RetryIntervalSeconds} seconds.",
                    _retryInterval.TotalSeconds);
            }

            try
            {
                await Task.Delay(_retryInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}