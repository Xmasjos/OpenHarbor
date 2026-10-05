using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OpenHarbor.Options;

namespace OpenHarbor.Services;

public class PluginPackageInstaller(IOptions<PluginServerOptions> options) : IPluginPackageInstaller
{
    private const int MaxArchiveEntries = 5000;
    private const long MaxExtractedBytes = 512 * 1024 * 1024;
    private readonly string _managedPluginRoot = Path.GetFullPath(options.Value.ManagedPluginRoot);
    private readonly long _maxUploadBytes = options.Value.MaxUploadBytes;

    public async Task<PluginPackage> InstallAsync(Guid pluginId, IFormFile package, CancellationToken cancellationToken)
    {
        if (pluginId == Guid.Empty)
            throw new InvalidOperationException("A plugin ID is required to install a package.");

        if (package.Length == 0 || package.Length > _maxUploadBytes)
            throw new InvalidOperationException($"The ZIP package must be between 1 byte and {_maxUploadBytes} bytes.");

        if (!string.Equals(Path.GetExtension(package.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The uploaded plugin package must be a ZIP file.");

        var pluginDirectory = Path.Combine(_managedPluginRoot, "bundles", pluginId.ToString("N"));
        var packageId = Guid.NewGuid().ToString("N");
        var stagingDirectory = Path.Combine(pluginDirectory, $".{packageId}.staging");
        var destinationDirectory = Path.Combine(pluginDirectory, packageId);
        Directory.CreateDirectory(stagingDirectory);

        try
        {
            await using var uploadStream = package.OpenReadStream();
            using var archive = new ZipArchive(uploadStream, ZipArchiveMode.Read);
            if (archive.Entries.Count == 0 || archive.Entries.Count > MaxArchiveEntries)
                throw new InvalidOperationException($"The ZIP package must contain between 1 and {MaxArchiveEntries} entries.");

            var dllPaths = new List<string>();
            string? publicDirectory = null;
            long extractedBytes = 0;

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var normalizedName = NormalizeEntryName(entry.FullName);
                var targetPath = GetContainedPath(stagingDirectory, normalizedName);
                if (IsSymbolicLink(entry))
                    throw new InvalidOperationException("ZIP packages cannot contain symbolic links.");

                if (IsDirectoryEntry(entry, normalizedName))
                {
                    Directory.CreateDirectory(targetPath);
                    if (IsPublicDirectory(normalizedName))
                        publicDirectory = normalizedName;

                    continue;
                }

                if (entry.Length < 0 || entry.Length > MaxExtractedBytes - extractedBytes)
                    throw new InvalidOperationException($"The extracted ZIP package cannot exceed {MaxExtractedBytes} bytes.");

                extractedBytes += entry.Length;

                var parentDirectory = Path.GetDirectoryName(targetPath);
                if (parentDirectory is not null)
                    Directory.CreateDirectory(parentDirectory);

                await using var source = entry.Open();
                await using var destination = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                await CopyEntryAsync(source, destination, cancellationToken);

                if (normalizedName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    && GetPublicDirectory(normalizedName) is null)
                {
                    dllPaths.Add(normalizedName);
                }

                var publicPath = GetPublicDirectory(normalizedName);
                if (publicPath is not null)
                    publicDirectory = publicPath;
            }

            if (dllPaths.Count != 1)
            {
                throw new InvalidOperationException("The ZIP package must contain exactly one DLL outside its public folder.");
            }

            Directory.Move(stagingDirectory, destinationDirectory);
            var dllRelativePath = GetManagedRelativePath(destinationDirectory, dllPaths[0]);
            var publicRelativePath = publicDirectory is null
                ? null
                : GetManagedRelativePath(destinationDirectory, publicDirectory);
            return new PluginPackage(dllRelativePath, publicRelativePath, destinationDirectory);
        }
        catch
        {
            if (Directory.Exists(stagingDirectory))
                Directory.Delete(stagingDirectory, recursive: true);

            if (Directory.Exists(destinationDirectory))
                Directory.Delete(destinationDirectory, recursive: true);

            throw;
        }
    }

    public Task RemoveAsync(PluginPackage package, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bundlesRoot = Path.GetFullPath(Path.Combine(_managedPluginRoot, "bundles"))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var packageDirectory = Path.GetFullPath(package.DirectoryPath);
        if (!packageDirectory.StartsWith(bundlesRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The package directory is outside managed bundle storage.");

        if (Directory.Exists(packageDirectory))
            Directory.Delete(packageDirectory, recursive: true);

        return Task.CompletedTask;
    }

    private static async Task CopyEntryAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
                break;

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }

    private static string NormalizeEntryName(string entryName)
    {
        var normalized = entryName.Replace('\\', '/');
        var path = normalized.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(path)
            || path.StartsWith('/')
            || path.Contains(':', StringComparison.Ordinal)
            || path.Contains('\0', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The ZIP package contains an invalid entry path.");
        }

        var segments = path.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new InvalidOperationException("The ZIP package cannot contain parent or empty path segments.");
        }

        return path;
    }

    private static string GetContainedPath(string root, string relativePath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The ZIP package contains a path outside its extraction directory.");

        return fullPath;
    }

    private static bool IsDirectoryEntry(ZipArchiveEntry entry, string normalizedName) =>
        entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\') || string.IsNullOrEmpty(entry.Name) || normalizedName.Length == 0;

    private static bool IsSymbolicLink(ZipArchiveEntry entry) =>
        ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000;

    private static bool IsPublicDirectory(string path) =>
        string.Equals(path, "public", StringComparison.OrdinalIgnoreCase);

    private static string? GetPublicDirectory(string path)
    {
        var publicIndex = path.IndexOf("/public/", StringComparison.OrdinalIgnoreCase);
        if (publicIndex >= 0)
            return path[..(publicIndex + "/public".Length)];

        return path.StartsWith("public/", StringComparison.OrdinalIgnoreCase) ? "public" : null;
    }

    private string GetManagedRelativePath(string packageDirectory, string relativePath) =>
        Path.GetRelativePath(_managedPluginRoot, Path.Combine(packageDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)))
            .Replace('\\', '/');
}