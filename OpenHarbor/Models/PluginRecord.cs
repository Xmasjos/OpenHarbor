using System.ComponentModel.DataAnnotations;

namespace OpenHarbor.Models;

public class PluginRecord : ICreatable, IUpdatable
{
    public Guid Id { get; set; }

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

    public bool IsDashboardProvider { get; set; }

    public bool IsSelectedDashboardProvider { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}
