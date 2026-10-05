namespace OpenHarbor.PluginContracts;

public enum PluginApplicationLaunchMode
{
    DashboardWindow,
    NewBrowserTab
}

public record PluginApplicationDescriptor(
    string Name,
    string IconSvg,
    PluginApplicationLaunchMode LaunchMode);

public record PluginDashboardCapability(string ProviderName);
