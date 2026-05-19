using System.ComponentModel.DataAnnotations;

namespace InventoryPilot.Models;

public class InventoryFieldDefinition
{
    public int Id { get; set; }
    public int InventoryId { get; set; }
    public Inventory? Inventory { get; set; }
    public int SortOrder { get; set; }

    [Required, StringLength(32)]
    public string FieldType { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(400)]
    public string Description { get; set; } = string.Empty;

    public bool ShowInTable { get; set; }
    public bool IsRequired { get; set; }

    [StringLength(256)]
    public string? RegexPattern { get; set; }

    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? MaxLength { get; set; }

    [StringLength(2000)]
    public string? OptionsJson { get; set; }

    public ICollection<ItemFieldValue> ItemValues { get; set; } = new List<ItemFieldValue>();
}
