using Microsoft.Extensions.Options;
using OpenHarbor.Models;
using OpenHarbor.Options;

namespace OpenHarbor.Services;

public class PluginRecordNormalizer(IOptions<PluginServerOptions> options) : IPluginRecordNormalizer
{
    private readonly string _managedPluginRoot = string.IsNullOrWhiteSpace(options.Value.ManagedPluginRoot)
        ? throw new InvalidOperationException("Managed plugin root is not configured.")
        : Path.GetFullPath(options.Value.ManagedPluginRoot);

    public void Normalize(PluginRecord record)
    {
        record.Name = record.Name.Trim();
        record.RouteSubpath = NormalizeRouteSubpath(record.RouteSubpath);
        record.DllRelativePath = NormalizeRelativePath(record.DllRelativePath);

        if (string.IsNullOrWhiteSpace(record.Name))
        {
            throw new InvalidOperationException("Plugin name is required.");
        }

        if (!string.IsNullOrWhiteSpace(record.PublicFolderRelativePath))
        {
            record.PublicFolderRelativePath = NormalizeRelativePath(record.PublicFolderRelativePath);
        }
        else
        {
            record.PublicFolderRelativePath = null;
        }
    }

    private static string NormalizeRouteSubpath(string routeSubpath)
    {
        var sanitized = (routeSubpath ?? string.Empty)
            .Trim()
            .Replace('\\', '/')
            .Trim('/')
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(sanitized))
            throw new InvalidOperationException("Route subpath is required.");

        if (sanitized.Contains("//", StringComparison.Ordinal) || sanitized.StartsWith('.'))
            throw new InvalidOperationException("Route subpath is not valid.");

        if (sanitized.StartsWith("plugins", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = sanitized["plugins".Length..].TrimStart('/');
        }

        return sanitized.Trim('/');
    }

    private string NormalizeRelativePath(string relativePath)
    {
        var cleaned = (relativePath ?? string.Empty).Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            throw new InvalidOperationException("A DLL path is required.");
        }

        if (cleaned.StartsWith("/", StringComparison.Ordinal))
        {
            cleaned = cleaned.TrimStart('/');
        }

        if (cleaned.Contains("..", StringComparison.Ordinal) || cleaned.Contains("\\", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Relative plugin file paths cannot use parent traversal.");
        }

        var candidate = Path.GetFullPath(Path.Combine(_managedPluginRoot, cleaned));
        var rootWithSeparator = _managedPluginRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!string.Equals(candidate, _managedPluginRoot, StringComparison.OrdinalIgnoreCase)
            && !candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The selected DLL path resolves outside the managed plugin root.");
        }

        return candidate[_managedPluginRoot.Length..]
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace('\\', '/');
    }
}