namespace InventoryPilot.Models.ViewModels;

public class UserAdminRowViewModel
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsBlocked { get; init; }
    public bool IsAdmin { get; init; }
}
