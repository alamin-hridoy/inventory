using InventoryPilot.Data;
using InventoryPilot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryPilot.Services;

public static class AppSeeder
{
    private const string InitialMigrationId = "20260515172052_InitialCreate";
    private const string EfProductVersion = "8.0.25";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await HasExistingIdentitySchemaAsync(db) && !await HasBaselineMigrationAsync(db))
        {
            await EnsureCompatibilitySchemaAsync(db);
            await MarkBaselineMigrationAsync(db);
        }
        else if ((await db.Database.GetPendingMigrationsAsync()).Any())
        {
            await db.Database.MigrateAsync();
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (!db.Tags.Any())
        {
            var seedTags = "equipment,library,hr,office,books,devices"
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => new Tag { Name = x.Trim() })
                .ToList();
            db.Tags.AddRange(seedTags);
        }

        if (!await db.InventoryCategories.AnyAsync())
        {
            db.InventoryCategories.AddRange(
                new InventoryCategory { Name = "Equipment" },
                new InventoryCategory { Name = "Furniture" },
                new InventoryCategory { Name = "Book" },
                new InventoryCategory { Name = "HR" },
                new InventoryCategory { Name = "Other" });
        }

        var adminEmail = "admin@inventorypilot.local";
        var admin = await userManager.Users.FirstOrDefaultAsync(x => x.Email == adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Inventory Admin"
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        if (!await db.Inventories.AnyAsync() && admin is not null)
        {
            var equipmentTag = await db.Tags.FirstAsync(x => x.Name == "equipment");
            var officeTag = await db.Tags.FirstAsync(x => x.Name == "office");
            var inventory = new Inventory
            {
                Title = "Office Equipment",
                Description = "Track shared laptops, monitors, chairs, and supporting equipment with custom inventory numbers.",
                Category = "Equipment",
                OwnerId = admin.Id,
                IsPublicWrite = true,
                Tags =
                [
                    new InventoryTag { Tag = equipmentTag },
                    new InventoryTag { Tag = officeTag }
                ],
                CustomIdElements =
                [
                    new InventoryCustomIdElement { ElementType = "Fixed", FixedText = "EQ-", SortOrder = 0 },
                    new InventoryCustomIdElement { ElementType = "Sequence", Format = "D4", SortOrder = 1 },
                    new InventoryCustomIdElement { ElementType = "Fixed", FixedText = "-", SortOrder = 2 },
                    new InventoryCustomIdElement { ElementType = "DateTime", Format = "yyyy", SortOrder = 3 }
                ],
                FieldDefinitions =
                [
                    new InventoryFieldDefinition { FieldType = InventoryFieldTypes.SingleLineText, Title = "Model", Description = "Vendor model name", ShowInTable = true, SortOrder = 0 },
                    new InventoryFieldDefinition { FieldType = InventoryFieldTypes.Number, Title = "Price", Description = "Purchase price", ShowInTable = true, SortOrder = 1, MinValue = 0 },
                    new InventoryFieldDefinition { FieldType = InventoryFieldTypes.Boolean, Title = "Assigned", Description = "Whether this asset is assigned", ShowInTable = true, SortOrder = 2 }
                ]
            };

            db.Inventories.Add(inventory);
            await db.SaveChangesAsync();

            var item = new ItemRecord
            {
                InventoryId = inventory.Id,
                CustomId = "EQ-0001-2025",
                DisplayName = "Dell Latitude 7440",
                SequenceNumber = 1,
                CreatedById = admin.Id,
                FieldValues =
                [
                    new ItemFieldValue { FieldDefinitionId = inventory.FieldDefinitions.First().Id, StringValue = "Latitude 7440" },
                    new ItemFieldValue { FieldDefinitionId = inventory.FieldDefinitions.Skip(1).First().Id, NumericValue = 1450 },
                    new ItemFieldValue { FieldDefinitionId = inventory.FieldDefinitions.Skip(2).First().Id, BoolValue = true }
                ]
            };

            db.ItemRecords.Add(item);
            db.InventoryDiscussionPosts.Add(new InventoryDiscussionPost
            {
                InventoryId = inventory.Id,
                UserId = admin.Id,
                Markdown = "**Welcome** to the equipment inventory. Use the table to manage shared assets."
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<bool> HasExistingIdentitySchemaAsync(ApplicationDbContext db) =>
        await db.Database
            .SqlQueryRaw<bool>("""
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_catalog.pg_class c
                    JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = 'public' AND c.relname = 'AspNetRoles'
                ) AS "Value"
                """)
            .SingleAsync();

    private static async Task<bool> HasBaselineMigrationAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );
            """);

        return await db.Database
            .SqlQueryRaw<bool>($"""
                SELECT EXISTS (
                    SELECT 1
                    FROM "__EFMigrationsHistory"
                    WHERE "MigrationId" = '{InitialMigrationId}'
                ) AS "Value"
                """)
            .SingleAsync();
    }

    private static async Task MarkBaselineMigrationAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('{InitialMigrationId}', '{EfProductVersion}')
            ON CONFLICT ("MigrationId") DO NOTHING;
            """);
    }

    private static async Task EnsureCompatibilitySchemaAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "ItemRecords"
            ADD COLUMN IF NOT EXISTS "SequenceNumber" integer NOT NULL DEFAULT 0;

            CREATE TABLE IF NOT EXISTS "InventoryCategories" (
                "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "Name" character varying(64) NOT NULL UNIQUE
            );

            INSERT INTO "InventoryCategories" ("Name")
            VALUES ('Equipment'), ('Furniture'), ('Book'), ('HR'), ('Other')
            ON CONFLICT ("Name") DO NOTHING;
            """);
    }
}
