using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenHarbor.Models;

namespace OpenHarbor.DAL;

public class EntityTimestampInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetTimestamps(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetTimestamps(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void SetTimestamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();
        var timestamp = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is ICreatable creatable)
                {
                    creatable.CreatedUtc = timestamp;
                }

                if (entry.Entity is IUpdatable updatable)
                {
                    updatable.UpdatedUtc = timestamp;
                }
            }
            else if (entry.State == EntityState.Modified && entry.Entity is IUpdatable updatable)
            {
                updatable.UpdatedUtc = timestamp;
                entry.Property(nameof(IUpdatable.UpdatedUtc)).IsModified = true;
            }
        }
    }
}