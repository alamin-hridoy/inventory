namespace InventoryPilot.Models.ViewModels;

public class ItemFieldInputModel
{
    public int FieldDefinitionId { get; set; }
    public string FieldType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? StringValue { get; set; }
    public decimal? NumericValue { get; set; }
    public bool? BoolValue { get; set; }
    public string? OptionsJson { get; set; }
}
