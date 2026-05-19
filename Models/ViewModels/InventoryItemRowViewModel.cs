namespace InventoryPilot.Models.ViewModels;

public class InventoryItemRowViewModel
{
    public int Id { get; init; }
    public int Version { get; init; }
    public string CustomId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public int LikeCount { get; init; }
    public Dictionary<int, string> VisibleFieldValues { get; init; } = [];
}
