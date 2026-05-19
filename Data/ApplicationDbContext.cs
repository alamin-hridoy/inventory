using InventoryPilot.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryPilot.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<InventoryTag> InventoryTags => Set<InventoryTag>();
    public DbSet<InventoryAccessGrant> InventoryAccessGrants => Set<InventoryAccessGrant>();
    public DbSet<InventoryDiscussionPost> InventoryDiscussionPosts => Set<InventoryDiscussionPost>();
    public DbSet<InventoryCustomIdElement> InventoryCustomIdElements => Set<InventoryCustomIdElement>();
    public DbSet<InventoryFieldDefinition> InventoryFieldDefinitions => Set<InventoryFieldDefinition>();
    public DbSet<ItemRecord> ItemRecords => Set<ItemRecord>();
    public DbSet<ItemFieldValue> ItemFieldValues => Set<ItemFieldValue>();
    public DbSet<ItemLike> ItemLikes => Set<ItemLike>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .Property(x => x.DisplayName)
            .HasMaxLength(120);

        builder.Entity<Inventory>()
            .Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Entity<InventoryCategory>()
            .HasIndex(x => x.Name)
            .IsUnique();

        builder.Entity<InventoryCategory>()
            .HasAlternateKey(x => x.Name);

        builder.Entity<Inventory>()
            .HasOne(x => x.CategoryLookup)
            .WithMany()
            .HasForeignKey(x => x.Category)
            .HasPrincipalKey(x => x.Name)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ItemRecord>()
            .Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Entity<InventoryTag>()
            .HasKey(x => new { x.InventoryId, x.TagId });

        builder.Entity<InventoryTag>()
            .HasOne(x => x.Inventory)
            .WithMany(x => x.Tags)
            .HasForeignKey(x => x.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InventoryTag>()
            .HasOne(x => x.Tag)
            .WithMany(x => x.Inventories)
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Inventory>()
            .HasOne(x => x.Owner)
            .WithMany(x => x.OwnedInventories)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<InventoryAccessGrant>()
            .HasOne(x => x.Inventory)
            .WithMany(x => x.AccessGrants)
            .HasForeignKey(x => x.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InventoryCustomIdElement>()
            .HasOne(x => x.Inventory)
            .WithMany(x => x.CustomIdElements)
            .HasForeignKey(x => x.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InventoryFieldDefinition>()
            .HasOne(x => x.Inventory)
            .WithMany(x => x.FieldDefinitions)
            .HasForeignKey(x => x.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InventoryDiscussionPost>()
            .HasOne(x => x.Inventory)
            .WithMany(x => x.DiscussionPosts)
            .HasForeignKey(x => x.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ItemRecord>()
            .HasOne(x => x.Inventory)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ItemFieldValue>()
            .HasOne(x => x.ItemRecord)
            .WithMany(x => x.FieldValues)
            .HasForeignKey(x => x.ItemRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ItemFieldValue>()
            .HasOne(x => x.FieldDefinition)
            .WithMany(x => x.ItemValues)
            .HasForeignKey(x => x.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ItemLike>()
            .HasOne(x => x.ItemRecord)
            .WithMany(x => x.Likes)
            .HasForeignKey(x => x.ItemRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ItemRecord>()
            .HasIndex(x => new { x.InventoryId, x.CustomId })
            .IsUnique();

        builder.Entity<ItemLike>()
            .HasIndex(x => new { x.ItemRecordId, x.UserId })
            .IsUnique();

        builder.Entity<InventoryAccessGrant>()
            .HasIndex(x => new { x.InventoryId, x.UserId })
            .IsUnique();

        builder.Entity<Tag>()
            .HasIndex(x => x.Name)
            .IsUnique();

        builder.Entity<ItemFieldValue>()
            .HasIndex(x => new { x.ItemRecordId, x.FieldDefinitionId })
            .IsUnique();
    }
}
