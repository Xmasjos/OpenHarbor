using Microsoft.EntityFrameworkCore;
using OpenHarbor.Plugins.Todo.DAL;
using OpenHarbor.Plugins.Todo.Models;

namespace OpenHarbor.Plugins.Todo.Services;

public class TodoListReader(TodoDbContext dbContext) : ITodoListReader
{
    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Todos.AsNoTracking().ToListAsync(cancellationToken);
        return records
            .OrderBy(todo => todo.IsFinished)
            .ThenBy(todo => todo.IsFinished ? todo.FinishedUtcTicks : todo.ActivePosition)
            .ThenBy(todo => todo.Id)
            .Select(ToItem)
            .ToList();
    }

    internal static TodoItem ToItem(TodoRecord record) => new(
        record.Id,
        record.Name,
        record.Description,
        record.IsFinished,
        record.ActivePosition,
        record.FinishedUtcTicks is long ticks ? new DateTimeOffset(ticks, TimeSpan.Zero) : null);
}