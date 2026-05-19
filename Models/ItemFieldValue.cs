namespace InventoryPilot.Models;

public class ItemFieldValue
{
    public int Id { get; set; }
    public int ItemRecordId { get; set; }
    public ItemRecord? ItemRecord { get; set; }
    public int FieldDefinitionId { get; set; }
    public InventoryFieldDefinition? FieldDefinition { get; set; }
    public string? StringValue { get; set; }
    public decimal? NumericValue { get; set; }
    public bool? BoolValue { get; set; }
}
