namespace InventoryPilot.Models.Integrations;

public class InventoryAggregateResult
{
    public int InventoryId { get; init; }
    public string InventoryTitle { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public DateTimeOffset GeneratedAtUtc { get; init; }
    public IReadOnlyList<InventoryFieldAggregate> Fields { get; init; } = [];
}

public class InventoryFieldAggregate
{
    public int FieldId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public decimal? Average { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public IReadOnlyList<InventoryTopValue> TopValues { get; set; } = [];
}

public record InventoryTopValue(string Value, int Count);
