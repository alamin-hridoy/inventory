using Microsoft.AspNetCore.Identity;

namespace InventoryPilot.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "en";
    public string PreferredTheme { get; set; } = "light";

    public ICollection<Inventory> OwnedInventories { get; set; } = new List<Inventory>();
    public ICollection<InventoryAccessGrant> AccessGrants { get; set; } = new List<InventoryAccessGrant>();
    public ICollection<InventoryDiscussionPost> DiscussionPosts { get; set; } = new List<InventoryDiscussionPost>();
    public ICollection<ItemRecord> CreatedItems { get; set; } = new List<ItemRecord>();
    public ICollection<ItemLike> ItemLikes { get; set; } = new List<ItemLike>();
}
