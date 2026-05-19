using System.ComponentModel.DataAnnotations;

namespace InventoryPilot.Models;

public class Inventory
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(64)]
    public string Category { get; set; } = "Other";
    public InventoryCategory? CategoryLookup { get; set; }

    [StringLength(512)]
    public string? ImageUrl { get; set; }

    public bool IsPublicWrite { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Version { get; set; } = 1;

    [Required]
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    public ICollection<InventoryTag> Tags { get; set; } = new List<InventoryTag>();
    public ICollection<InventoryAccessGrant> AccessGrants { get; set; } = new List<InventoryAccessGrant>();
    public ICollection<InventoryCustomIdElement> CustomIdElements { get; set; } = new List<InventoryCustomIdElement>();
    public ICollection<InventoryFieldDefinition> FieldDefinitions { get; set; } = new List<InventoryFieldDefinition>();
    public ICollection<ItemRecord> Items { get; set; } = new List<ItemRecord>();
    public ICollection<InventoryDiscussionPost> DiscussionPosts { get; set; } = new List<InventoryDiscussionPost>();
}
