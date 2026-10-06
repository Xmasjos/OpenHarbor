namespace OpenHarbor.Plugins.Todo.Models;

public record TodoItem(
    Guid Id,
    string Name,
    string? Description,
    bool IsFinished,
    int Position,
    DateTimeOffset? FinishedUtc);