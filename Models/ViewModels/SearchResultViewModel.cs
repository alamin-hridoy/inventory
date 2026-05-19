namespace InventoryPilot.Models.ViewModels;

public class SearchResultViewModel
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<InventorySummaryViewModel> Inventories { get; init; } = [];
    public IReadOnlyList<ItemSearchHitViewModel> Items { get; init; } = [];
}
