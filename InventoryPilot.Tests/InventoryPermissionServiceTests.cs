using InventoryPilot.Models;
using InventoryPilot.Services;

namespace InventoryPilot.Tests;

public class InventoryPermissionServiceTests
{
    private readonly InventoryPermissionService _permissions = new();

    [Fact]
    public void CanEditInventory_AllowsOwner()
    {
        var inventory = new Inventory { OwnerId = "owner-id" };

        Assert.True(_permissions.CanEditInventory(inventory, "owner-id", isAdmin: false));
    }

    [Fact]
    public void CanEditInventory_AllowsAdminEvenWhenNotOwner()
    {
        var inventory = new Inventory { OwnerId = "owner-id" };

        Assert.True(_permissions.CanEditInventory(inventory, "other-user", isAdmin: true));
    }

    [Fact]
    public void CanEditItems_AllowsGrantedUser()
    {
        var inventory = new Inventory
        {
            OwnerId = "owner-id",
            AccessGrants = [new InventoryAccessGrant { UserId = "writer-id" }]
        };

        Assert.True(_permissions.CanEditItems(inventory, "writer-id", isAdmin: false));
    }

    [Fact]
    public void CanEditItems_AllowsAnyAuthenticatedUserForPublicWriteInventory()
    {
        var inventory = new Inventory { OwnerId = "owner-id", IsPublicWrite = true };

        Assert.True(_permissions.CanEditItems(inventory, "signed-in-user", isAdmin: false));
    }

    [Fact]
    public void CanEditItems_DeniesAnonymousUserEvenForPublicWriteInventory()
    {
        var inventory = new Inventory { OwnerId = "owner-id", IsPublicWrite = true };

        Assert.False(_permissions.CanEditItems(inventory, null, isAdmin: false));
    }
}
