using Microsoft.EntityFrameworkCore;
using OpenHarbor.Plugins.Todo.DAL;
using OpenHarbor.Plugins.Todo.Models;

namespace OpenHarbor.Plugins.Todo.Services;

public class TodoListWriter(TodoDbContext dbContext, TimeProvider timeProvider) : ITodoListWriter
{
    public async Task<TodoItem> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Description);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Todos.Where(todo => !todo.IsFinished)
            .ExecuteUpdateAsync(update => update.SetProperty(todo => todo.ActivePosition, todo => todo.ActivePosition + 1), cancellationToken);

        var record = new TodoRecord
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            ActivePosition = 0
        };
        dbContext.Todos.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TodoListReader.ToItem(record);
    }

    public async Task<TodoItem?> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Description);
        var record = await dbContext.Todos.FirstOrDefaultAsync(todo => todo.Id == id, cancellationToken);
        if (record is null)
            return null;

        record.Name = request.Name;
        record.Description = request.Description;
        await dbContext.SaveChangesAsync(cancellationToken);
        return TodoListReader.ToItem(record);
    }

    public async Task<TodoItem?> SetFinishedAsync(Guid id, bool isFinished, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await dbContext.Todos.FirstOrDefaultAsync(todo => todo.Id == id, cancellationToken);
        if (record is null)
            return null;

        if (record.IsFinished != isFinished)
        {
            if (isFinished)
            {
                record.IsFinished = true;
                var latestCompletion = await dbContext.Todos
                    .Where(todo => todo.IsFinished && todo.FinishedUtcTicks.HasValue)
                    .MaxAsync(todo => todo.FinishedUtcTicks, cancellationToken);
                var completedAt = timeProvider.GetUtcNow().UtcTicks;
                record.FinishedUtcTicks = latestCompletion is long latest && completedAt <= latest
                    ? latest + 1
                    : completedAt;
                await NormalizeActivePositionsAsync(cancellationToken, id);
            }
            else
            {
                await dbContext.Todos.Where(todo => !todo.IsFinished)
                    .ExecuteUpdateAsync(update => update.SetProperty(todo => todo.ActivePosition, todo => todo.ActivePosition + 1), cancellationToken);
                record.IsFinished = false;
                record.FinishedUtcTicks = null;
                record.ActivePosition = 0;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return TodoListReader.ToItem(record);
    }

    public async Task ReorderActiveAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        var active = await dbContext.Todos.Where(todo => !todo.IsFinished)
            .OrderBy(todo => todo.ActivePosition)
            .ToListAsync(cancellationToken);
        if (ids.Count != active.Count || ids.Distinct().Count() != ids.Count || !ids.ToHashSet().SetEquals(active.Select(todo => todo.Id)))
            throw new TodoValidationException("The active Todo order is outdated or invalid.");

        var recordsById = active.ToDictionary(todo => todo.Id);
        for (var position = 0; position < ids.Count; position++)
            recordsById[ids[position]].ActivePosition = position;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await dbContext.Todos.FirstOrDefaultAsync(todo => todo.Id == id, cancellationToken);
        if (record is null)
            return false;

        dbContext.Todos.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (!record.IsFinished)
            await NormalizeActivePositionsAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task NormalizeActivePositionsAsync(CancellationToken cancellationToken, Guid? excludedId = null)
    {
        var query = dbContext.Todos.Where(todo => !todo.IsFinished);
        if (excludedId is Guid id)
            query = query.Where(todo => todo.Id != id);

        var active = await query
            .OrderBy(todo => todo.ActivePosition)
            .ToListAsync(cancellationToken);
        for (var position = 0; position < active.Count; position++)
            active[position].ActivePosition = position;
    }

    private static void Validate(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new TodoValidationException("A Todo name must contain a non-whitespace character.");
        if (name.Length > 256)
            throw new TodoValidationException("A Todo name cannot exceed 256 characters.");
        if (description?.Length > 10000)
            throw new TodoValidationException("A Todo description cannot exceed 10,000 characters.");
    }
}