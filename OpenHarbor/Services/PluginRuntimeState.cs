namespace OpenHarbor.Services;

public class PluginRuntimeState
{
    private readonly List<PluginRuntimeStatus> _statuses = [];

    public bool RestartPending { get; private set; }

    public IReadOnlyList<PluginRuntimeStatus> Statuses => _statuses;

    public void ReplaceStatuses(IEnumerable<PluginRuntimeStatus> statuses)
    {
        _statuses.Clear();
        _statuses.AddRange(statuses);
        RestartPending = false;
    }

    public void MarkRestartPending() => RestartPending = true;
}

public record PluginRuntimeStatus(string Key, string Name, string Status, string? Diagnostic);
