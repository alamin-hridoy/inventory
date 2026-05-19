using System.ComponentModel.DataAnnotations;

namespace InventoryPilot.Models;

public class InventoryCustomIdElement
{
    public int Id { get; set; }
    public int InventoryId { get; set; }
    public Inventory? Inventory { get; set; }
    public int SortOrder { get; set; }

    [Required, StringLength(40)]
    public string ElementType { get; set; } = string.Empty;

    [StringLength(256)]
    public string? FixedText { get; set; }

    [StringLength(128)]
    public string? Format { get; set; }
}
