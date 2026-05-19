using System.ComponentModel.DataAnnotations;

namespace InventoryPilot.Models;

public class ItemRecord
{
    public int Id { get; set; }
    public int InventoryId { get; set; }
    public Inventory? Inventory { get; set; }

    [Required, StringLength(160)]
    public string CustomId { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    public int SequenceNumber { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Version { get; set; } = 1;

    public ICollection<ItemFieldValue> FieldValues { get; set; } = new List<ItemFieldValue>();
    public ICollection<ItemLike> Likes { get; set; } = new List<ItemLike>();
}
