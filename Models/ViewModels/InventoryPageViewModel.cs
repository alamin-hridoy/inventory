namespace InventoryPilot.Models.ViewModels;

public class InventoryPageViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DescriptionHtml { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public IReadOnlyList<string> Categories { get; init; } = [];
    public string? ImageUrl { get; init; }
    public string? CloudinaryCloudName { get; init; }
    public string? CloudinaryUploadPreset { get; init; }
    public string OwnerName { get; init; } = string.Empty;
    public bool IsPublicWrite { get; init; }
    public bool CanEditInventory { get; init; }
    public bool CanEditItems { get; init; }
    public int Version { get; init; }
    public string ActiveTab { get; init; } = "items";
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<InventoryFieldDefinitionViewModel> Fields { get; init; } = [];
    public IReadOnlyList<InventoryCustomIdElementViewModel> CustomIdElements { get; init; } = [];
    public IReadOnlyList<InventoryItemRowViewModel> Items { get; init; } = [];
    public IReadOnlyList<InventoryDiscussionPostViewModel> DiscussionPosts { get; init; } = [];
    public IReadOnlyList<InventoryAccessGrantViewModel> AccessUsers { get; init; } = [];
    public InventoryStatisticsViewModel Statistics { get; init; } = new();
    public string ApiToken { get; init; } = string.Empty;
    public string ApiUrl { get; init; } = string.Empty;
}
