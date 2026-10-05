using Microsoft.AspNetCore.Http;

namespace OpenHarbor.Services;

public interface IPluginPackageInstaller
{
    Task<PluginPackage> InstallAsync(Guid pluginId, IFormFile package, CancellationToken cancellationToken);

    Task RemoveAsync(PluginPackage package, CancellationToken cancellationToken);
}

public record PluginPackage(string DllRelativePath, string? PublicFolderRelativePath, string DirectoryPath);