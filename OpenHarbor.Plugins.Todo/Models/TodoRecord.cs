namespace OpenHarbor.Plugins.Todo.Models;

public class TodoRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsFinished { get; set; }

    public int ActivePosition { get; set; }

    public long? FinishedUtcTicks { get; set; }
}