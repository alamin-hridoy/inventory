namespace InventoryPilot.Models.ViewModels;

public class ItemSearchHitViewModel
{
    public int ItemId { get; init; }
    public int InventoryId { get; init; }
    public string InventoryTitle { get; init; } = string.Empty;
    public string CustomId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
