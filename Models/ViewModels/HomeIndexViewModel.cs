namespace InventoryPilot.Models.ViewModels;

public class HomeIndexViewModel
{
    public IReadOnlyList<InventorySummaryViewModel> LatestInventories { get; init; } = [];
    public IReadOnlyList<InventorySummaryViewModel> PopularInventories { get; init; } = [];
    public IReadOnlyList<TagCloudEntryViewModel> TagCloud { get; init; } = [];
}
