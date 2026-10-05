using Microsoft.EntityFrameworkCore;
using OpenHarbor.DAL;
using OpenHarbor.Models;

namespace OpenHarbor.Services;

public class PluginCatalogWriter(
    PluginCatalogDbContext dbContext,
    PluginRuntimeState runtimeState,
    IPluginRecordNormalizer recordNormalizer,
    TimeProvider timeProvider,
    ILogger<PluginCatalogWriter> logger) : IPluginCatalogWriter
{
    public async Task<PluginRecord> CreateAsync(PluginRecord record, CancellationToken cancellationToken = default)
    {
        recordNormalizer.Normalize(record);

        if (await dbContext.Plugins.AnyAsync(x => x.RouteSubpath.Equals(record.RouteSubpath), cancellationToken))
            throw new InvalidOperationException("A plugin with that route subpath already exists.");

        dbContext.Plugins.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        runtimeState.MarkRestartPending();
        logger.LogInformation("Created plugin record {PluginName} at route {RouteSubpath}.", record.Name, record.RouteSubpath);
        return record;
    }

    public async Task<PluginRecord> UpdateAsync(PluginRecord record, CancellationToken cancellationToken = default)
    {
        recordNormalizer.Normalize(record);

        var existing = await dbContext.Plugins.FirstOrDefaultAsync(x => x.Id == record.Id, cancellationToken)
            ?? throw new InvalidOperationException("Plugin record not found.");

        if (await dbContext.Plugins.AnyAsync(x => x.Id != record.Id && x.RouteSubpath.Equals(record.RouteSubpath), cancellationToken))
            throw new InvalidOperationException("A plugin with that route subpath already exists.");

        if (existing.IsSelectedDashboardProvider && !record.IsDashboardProvider)
            throw new InvalidOperationException("Select another dashboard provider before removing this plugin's dashboard-provider capability.");

        existing.Name = record.Name;
        existing.RouteSubpath = record.RouteSubpath;
        existing.DllRelativePath = record.DllRelativePath;
        existing.PublicFolderRelativePath = record.PublicFolderRelativePath;
        existing.Enabled = record.Enabled;
        existing.IsDashboardProvider = record.IsDashboardProvider;
        await dbContext.SaveChangesAsync(cancellationToken);
        runtimeState.MarkRestartPending();
        logger.LogInformation("Updated plugin record {PluginName}.", existing.Name);
        return existing;
    }

    public async Task SelectDashboardProviderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var candidate = await dbContext.Plugins.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Plugin record not found.");

        if (!candidate.Enabled || !candidate.IsDashboardProvider)
            throw new InvalidOperationException("Only enabled dashboard providers can be selected.");

        var updatedUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.Plugins
            .Where(x => x.IsSelectedDashboardProvider)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(x => x.IsSelectedDashboardProvider, false)
                .SetProperty(x => x.UpdatedUtc, updatedUtc), cancellationToken);

        var selectedCount = await dbContext.Plugins
            .Where(x => x.Id == id && x.Enabled && x.IsDashboardProvider)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(x => x.IsSelectedDashboardProvider, true)
                .SetProperty(x => x.UpdatedUtc, updatedUtc), cancellationToken);

        if (selectedCount != 1)
            throw new InvalidOperationException("The dashboard provider is no longer available for selection.");

        await transaction.CommitAsync(cancellationToken);
        runtimeState.MarkRestartPending();
        logger.LogInformation("Selected plugin {PluginName} as the dashboard provider.", candidate.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Plugins.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Plugin record not found.");

        dbContext.Plugins.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        runtimeState.MarkRestartPending();
        logger.LogInformation("Deleted plugin record {PluginName}.", record.Name);
    }
}