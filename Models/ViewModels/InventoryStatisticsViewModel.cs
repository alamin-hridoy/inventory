namespace InventoryPilot.Models.ViewModels;

public class InventoryStatisticsViewModel
{
    public int ItemCount { get; init; }
    public IReadOnlyList<NumericFieldStatsViewModel> NumericFields { get; init; } = [];
    public IReadOnlyList<TopStringFieldStatsViewModel> StringFields { get; init; } = [];
}
