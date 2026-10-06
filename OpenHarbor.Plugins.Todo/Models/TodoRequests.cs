namespace OpenHarbor.Plugins.Todo.Models;

public record CreateTodoRequest(string Name, string? Description);

public record UpdateTodoRequest(string Name, string? Description);

public record SetTodoFinishedRequest(bool IsFinished);

public record ReorderTodosRequest(IReadOnlyList<Guid> Ids);

public class TodoValidationException(string message) : Exception(message);