using System.ComponentModel.DataAnnotations;

namespace InventoryPilot.Models.ViewModels;

public class ItemEditViewModel
{
    public int? Id { get; set; }
    public int InventoryId { get; set; }
    public int Version { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    [Required, StringLength(160)]
    public string CustomId { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    public List<ItemFieldInputModel> Fields { get; set; } = [];
}
