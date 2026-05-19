using InventoryPilot.Models;

namespace InventoryPilot.Services;

public class InventoryPermissionService
{
    public bool CanEditInventory(Inventory inventory, string? userId, bool isAdmin)
        => isAdmin || (!string.IsNullOrWhiteSpace(userId) && inventory.OwnerId == userId);

    public bool CanEditItems(Inventory inventory, string? userId, bool isAdmin)
        => isAdmin
           || (!string.IsNullOrWhiteSpace(userId)
               && (inventory.OwnerId == userId
                   || inventory.IsPublicWrite
                   || inventory.AccessGrants.Any(x => x.UserId == userId)));
}
