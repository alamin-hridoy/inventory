namespace InventoryPilot.Models.Integrations;

public class SupportTicketPayload
{
    public string ReportedBy { get; init; } = string.Empty;
    public string? Inventory { get; init; }
    public string Link { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> AdminEmails { get; init; } = [];
    public DateTimeOffset CreatedAtUtc { get; init; }
}
