namespace OpenHarbor.Plugins.Todo.Options;

public class TodoOptions
{
    public const string SectionName = "Todo";

    public string DatabasePath { get; set; } = Path.Combine("..", "data", "todo.db");
}