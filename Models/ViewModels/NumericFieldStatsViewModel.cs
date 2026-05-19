namespace InventoryPilot.Models.ViewModels;

public class NumericFieldStatsViewModel
{
    public string Title { get; init; } = string.Empty;
    public decimal? Average { get; init; }
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
}
