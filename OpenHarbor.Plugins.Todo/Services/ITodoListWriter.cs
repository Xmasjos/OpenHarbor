using OpenHarbor.Plugins.Todo.Models;

namespace OpenHarbor.Plugins.Todo.Services;

public interface ITodoListWriter
{
    Task<TodoItem> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default);

    Task<TodoItem?> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken cancellationToken = default);

    Task<TodoItem?> SetFinishedAsync(Guid id, bool isFinished, CancellationToken cancellationToken = default);

    Task ReorderActiveAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}