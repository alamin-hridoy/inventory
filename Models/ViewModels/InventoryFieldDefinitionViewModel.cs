namespace InventoryPilot.Models.ViewModels;

public class InventoryFieldDefinitionViewModel
{
    public int Id { get; init; }
    public string FieldType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool ShowInTable { get; init; }
    public bool IsRequired { get; init; }
    public int SortOrder { get; init; }
    public string? RegexPattern { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public int? MaxLength { get; init; }
    public string? OptionsJson { get; init; }
}
