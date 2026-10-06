using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OpenHarbor.Plugins.Todo.DAL;

namespace OpenHarbor.Plugins.Todo.Services;

public class TodoDatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<TodoDatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Todo database initialization failed. Todo requests will remain unavailable until storage is restored.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}