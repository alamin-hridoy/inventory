namespace InventoryPilot.Models;

public class ItemLike
{
    public int Id { get; set; }
    public int ItemRecordId { get; set; }
    public ItemRecord? ItemRecord { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
