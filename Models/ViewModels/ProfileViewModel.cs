namespace InventoryPilot.Models.ViewModels;

public class ProfileViewModel
{
    public string DisplayName { get; init; } = string.Empty;
    public IReadOnlyList<InventorySummaryViewModel> OwnedInventories { get; init; } = [];
    public IReadOnlyList<InventorySummaryViewModel> WritableInventories { get; init; } = [];
}
