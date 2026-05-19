using System.ComponentModel.DataAnnotations;

namespace InventoryPilot.Models.ViewModels;

public class InventoryEditInputModel
{
    public int? Id { get; set; }
    public int Version { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(64)]
    public string Category { get; set; } = "Other";

    [StringLength(512)]
    public string? ImageUrl { get; set; }

    public bool IsPublicWrite { get; set; }
    public string Tags { get; set; } = string.Empty;
    public IReadOnlyList<string> Categories { get; set; } = [];
    public string? CloudinaryCloudName { get; set; }
    public string? CloudinaryUploadPreset { get; set; }
}
