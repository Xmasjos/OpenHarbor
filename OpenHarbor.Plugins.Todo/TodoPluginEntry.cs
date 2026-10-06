using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenHarbor.PluginContracts;
using OpenHarbor.Plugins.Todo.DAL;
using OpenHarbor.Plugins.Todo.Models;
using OpenHarbor.Plugins.Todo.Options;
using OpenHarbor.Plugins.Todo.Services;

namespace OpenHarbor.Plugins.Todo;

public class TodoPluginEntry : IPluginEntry, IPluginDataCleanup
{
    private string? _databasePath;

    public string Name => "Todo";

    public string Version => "1.0.0";

    public PluginApplicationDescriptor? Application => new(
        "Todo",
        "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\"><path d=\"M5 12.5 9.5 17 19 7.5\" fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"/></svg>",
        PluginApplicationLaunchMode.DashboardWindow);

    public PluginDashboardCapability? Dashboard => null;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(TodoOptions.SectionName).Get<TodoOptions>() ?? new TodoOptions();
        _databasePath = Path.GetFullPath(options.DatabasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        var databasePath = _databasePath;

        services.AddDbContext<TodoDbContext>(database => database.UseSqlite($"Data Source={databasePath}"));
        services.AddScoped<ITodoListReader, TodoListReader>();
        services.AddScoped<ITodoListWriter, TodoListWriter>();
        services.AddHostedService<TodoDatabaseInitializer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/todos").RequireAuthorization("Admin");
        api.MapGet("/", async (ITodoListReader reader, CancellationToken cancellationToken) =>
            Results.Ok(await reader.GetAllAsync(cancellationToken)));
        api.MapPost("/", async (CreateTodoRequest request, ITodoListWriter writer, CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await writer.CreateAsync(request, cancellationToken));
            }
            catch (TodoValidationException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });
        api.MapPut("/{id:guid}", async (Guid id, UpdateTodoRequest request, ITodoListWriter writer, CancellationToken cancellationToken) =>
        {
            try
            {
                var todo = await writer.UpdateAsync(id, request, cancellationToken);
                return todo is null ? Results.NotFound() : Results.Ok(todo);
            }
            catch (TodoValidationException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });
        api.MapPut("/{id:guid}/finished", async (Guid id, SetTodoFinishedRequest request, ITodoListWriter writer, CancellationToken cancellationToken) =>
        {
            var todo = await writer.SetFinishedAsync(id, request.IsFinished, cancellationToken);
            return todo is null ? Results.NotFound() : Results.Ok(todo);
        });
        api.MapPut("/order", async (ReorderTodosRequest request, ITodoListWriter writer, CancellationToken cancellationToken) =>
        {
            try
            {
                await writer.ReorderActiveAsync(request.Ids, cancellationToken);
                return Results.NoContent();
            }
            catch (TodoValidationException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });
        api.MapDelete("/{id:guid}", async (Guid id, ITodoListWriter writer, CancellationToken cancellationToken) =>
            await writer.DeleteAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());
    }

    public Task DeleteDataAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_databasePath is null)
            throw new InvalidOperationException("Todo database storage was not configured.");

        File.Delete(_databasePath + "-wal");
        File.Delete(_databasePath + "-shm");
        File.Delete(_databasePath + "-journal");
        File.Delete(_databasePath);
        return Task.CompletedTask;
    }
}