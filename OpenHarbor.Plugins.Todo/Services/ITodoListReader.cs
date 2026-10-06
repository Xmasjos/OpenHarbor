using OpenHarbor.Plugins.Todo.Models;

namespace OpenHarbor.Plugins.Todo.Services;

public interface ITodoListReader
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);
}