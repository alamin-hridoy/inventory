namespace InventoryPilot.Models.ViewModels;

public class InventoryDiscussionPostViewModel
{
    public int Id { get; init; }
    public string AuthorId { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string Markdown { get; init; } = string.Empty;
    public string Html { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
