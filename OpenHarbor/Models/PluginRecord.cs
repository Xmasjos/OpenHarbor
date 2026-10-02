using System.ComponentModel.DataAnnotations;

namespace OpenHarbor.Models;

public class PluginRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string RouteSubpath { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string DllRelativePath { get; set; } = string.Empty;

    [StringLength(500)]
    public string? PublicFolderRelativePath { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
