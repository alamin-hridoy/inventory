namespace InventoryPilot.Models.ViewModels;

public class InventoryCustomIdElementViewModel
{
    public int Id { get; init; }
    public string ElementType { get; init; } = string.Empty;
    public string? FixedText { get; init; }
    public string? Format { get; init; }
    public int SortOrder { get; init; }
}
