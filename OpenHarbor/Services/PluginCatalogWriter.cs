using Microsoft.EntityFrameworkCore;
using OpenHarbor.Data;
using OpenHarbor.Models;

namespace OpenHarbor.Services;

public class PluginCatalogWriter(
    PluginCatalogDbContext dbContext,
    PluginRuntimeState runtimeState,
    IPluginRecordNormalizer recordNormalizer,
    ILogger<PluginCatalogWriter> logger) : IPluginCatalogWriter
{
    public async Task<PluginRecord> CreateAsync(PluginRecord record, CancellationToken cancellationToken = default)
    {
        recordNormalizer.Normalize(record);

        if (await dbContext.Plugins.AnyAsync(x => x.RouteSubpath == record.RouteSubpath, cancellationToken))
        {
            throw new InvalidOperationException("A plugin with that route subpath already exists.");
        }

        record.CreatedUtc = DateTime.UtcNow;
        record.UpdatedUtc = record.CreatedUtc;

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

        if (await dbContext.Plugins.AnyAsync(x => x.Id != record.Id && x.RouteSubpath == record.RouteSubpath, cancellationToken))
        {
            throw new InvalidOperationException("A plugin with that route subpath already exists.");
        }

        existing.Name = record.Name;
        existing.RouteSubpath = record.RouteSubpath;
        existing.DllRelativePath = record.DllRelativePath;
        existing.PublicFolderRelativePath = record.PublicFolderRelativePath;
        existing.Enabled = record.Enabled;
        existing.UpdatedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        runtimeState.MarkRestartPending();
        logger.LogInformation("Updated plugin record {PluginName}.", existing.Name);
        return existing;
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