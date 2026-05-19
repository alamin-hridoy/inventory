namespace InventoryPilot.Models.ViewModels;

public class InventorySummaryViewModel
{
    public int Id { get; init; }
    public int Version { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int ItemCount { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}
