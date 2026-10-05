using OpenHarbor.Models;

namespace OpenHarbor.Services;

public class PluginRuntimeState
{
    private readonly List<PluginRuntimeStatus> _statuses = [];

    public bool RestartPending { get; private set; }

    public Guid? SelectedDashboardPluginId { get; private set; }

    public string? SelectedDashboardPluginName { get; private set; }

    public string? SelectedDashboardRouteSubpath { get; private set; }

    public IReadOnlyList<PluginRuntimeStatus> Statuses => _statuses;

    public void ReplaceStatuses(IEnumerable<PluginRuntimeStatus> statuses)
    {
        _statuses.Clear();
        _statuses.AddRange(statuses);
        RestartPending = false;
    }

    public void SetSelectedDashboard(PluginRecord? selectedDashboard)
    {
        if (selectedDashboard is null)
        {
            SelectedDashboardPluginId = null;
            SelectedDashboardPluginName = null;
            SelectedDashboardRouteSubpath = null;
            return;
        }

        SelectedDashboardPluginId = selectedDashboard.Id;
        SelectedDashboardPluginName = selectedDashboard.Name;
        SelectedDashboardRouteSubpath = selectedDashboard.RouteSubpath.Trim('/');
    }

    public void MarkRestartPending() => RestartPending = true;
}

public record PluginRuntimeStatus(string Key, string Name, string Status, string? Diagnostic, string? Version = null);
