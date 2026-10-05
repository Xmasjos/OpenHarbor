namespace OpenHarbor.Options;

public class PluginServerOptions
{
    public const string SectionName = "PluginServer";

    public string ManagedPluginRoot { get; set; } = Path.Combine(AppContext.BaseDirectory, "managed-plugins");

    public int RuntimeStateInitializationRetryIntervalSeconds { get; set; } = 30;

    public long MaxUploadBytes { get; set; } = 64 * 1024 * 1024;

    public string AuthUsernameEnvironmentVariable { get; set; } = "PLUGIN_ADMIN_USERNAME";

    public string AuthPasswordEnvironmentVariable { get; set; } = "PLUGIN_ADMIN_PASSWORD";
}
