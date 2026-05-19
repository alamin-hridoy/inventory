using InventoryPilot.Data;
using InventoryPilot.Extensions;
using InventoryPilot.Models;
using InventoryPilot.Models.ViewModels;
using InventoryPilot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InventoryPilot.Controllers;

[Authorize]
public class InventoriesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly InventoryPermissionService _permissions;
    private readonly CustomIdComposer _customIdComposer;
    private readonly MarkdownRenderer _markdownRenderer;
    private readonly IConfiguration _configuration;

    public InventoriesController(
        ApplicationDbContext db,
        InventoryPermissionService permissions,
        CustomIdComposer customIdComposer,
        MarkdownRenderer markdownRenderer,
        IConfiguration configuration)
    {
        _db = db;
        _permissions = permissions;
        _customIdComposer = customIdComposer;
        _markdownRenderer = markdownRenderer;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id, string tab = "items")
    {
        var inventory = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Tags).ThenInclude(x => x.Tag)
            .Include(x => x.AccessGrants).ThenInclude(x => x.User)
            .Include(x => x.CustomIdElements)
            .Include(x => x.FieldDefinitions)
            .Include(x => x.DiscussionPosts).ThenInclude(x => x.User)
            .Include(x => x.Items).ThenInclude(x => x.Likes)
            .Include(x => x.Items).ThenInclude(x => x.FieldValues)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inventory is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var visibleFields = inventory.FieldDefinitions.Where(x => x.ShowInTable).OrderBy(x => x.SortOrder).ToList();
        var categories = await GetCategoriesAsync();
        var canEditInventory = _permissions.CanEditInventory(inventory, userId, isAdmin);
        var canEditItems = _permissions.CanEditItems(inventory, userId, isAdmin);
        var activeTab = tab.ToLowerInvariant();
        if (!canEditInventory && activeTab is "settings" or "customid" or "fields" or "access")
        {
            activeTab = "items";
        }

        var model = new InventoryPageViewModel
        {
            Id = inventory.Id,
            Title = inventory.Title,
            Description = inventory.Description,
            DescriptionHtml = _markdownRenderer.Render(inventory.Description),
            Category = inventory.Category,
            Categories = categories,
            ImageUrl = inventory.ImageUrl,
            CloudinaryCloudName = _configuration["Cloudinary:CloudName"],
            CloudinaryUploadPreset = _configuration["Cloudinary:UploadPreset"],
            OwnerName = inventory.Owner?.DisplayName ?? inventory.Owner?.Email ?? "Unknown",
            IsPublicWrite = inventory.IsPublicWrite,
            CanEditInventory = canEditInventory,
            CanEditItems = canEditItems,
            Version = inventory.Version,
            ActiveTab = activeTab,
            Tags = inventory.Tags.Select(x => x.Tag?.Name ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
            Fields = inventory.FieldDefinitions.OrderBy(x => x.SortOrder).Select(x => new InventoryFieldDefinitionViewModel
            {
                Id = x.Id,
                FieldType = x.FieldType,
                Title = x.Title,
                Description = x.Description,
                ShowInTable = x.ShowInTable,
                IsRequired = x.IsRequired,
                SortOrder = x.SortOrder,
                RegexPattern = x.RegexPattern,
                MinValue = x.MinValue,
                MaxValue = x.MaxValue,
                MaxLength = x.MaxLength,
                OptionsJson = x.OptionsJson
            }).ToList(),
            CustomIdElements = inventory.CustomIdElements.OrderBy(x => x.SortOrder).Select(x => new InventoryCustomIdElementViewModel
            {
                Id = x.Id,
                ElementType = x.ElementType,
                FixedText = x.FixedText,
                Format = x.Format,
                SortOrder = x.SortOrder
            }).ToList(),
            Items = inventory.Items.OrderByDescending(x => x.CreatedAt).Select(x => new InventoryItemRowViewModel
            {
                Id = x.Id,
                Version = x.Version,
                CustomId = x.CustomId,
                DisplayName = x.DisplayName,
                CreatedAt = x.CreatedAt,
                LikeCount = x.Likes.Count,
                VisibleFieldValues = visibleFields.ToDictionary(
                    field => field.Id,
                    field => FormatField(x.FieldValues.FirstOrDefault(v => v.FieldDefinitionId == field.Id)))
            }).ToList(),
            DiscussionPosts = inventory.DiscussionPosts.OrderBy(x => x.CreatedAt).Select(x => new InventoryDiscussionPostViewModel
            {
                Id = x.Id,
                AuthorId = x.UserId,
                AuthorName = x.User?.DisplayName ?? x.User?.Email ?? "Unknown",
                Markdown = x.Markdown,
                Html = _markdownRenderer.Render(x.Markdown),
                CreatedAt = x.CreatedAt
            }).ToList(),
            AccessUsers = inventory.AccessGrants.Select(x => new InventoryAccessGrantViewModel
            {
                UserId = x.UserId,
                DisplayName = x.User?.DisplayName ?? string.Empty,
                Email = x.User?.Email ?? string.Empty
            }).OrderBy(x => x.DisplayName).ToList(),
            Statistics = BuildStatistics(inventory)
        };

        ViewBag.CustomIdPreview = _customIdComposer.BuildPreview(
            inventory.CustomIdElements,
            inventory.Items.Select(x => x.SequenceNumber).DefaultIfEmpty(0).Max() + 1);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? id = null)
    {
        if (id is null or <= 0)
        {
            return View("Edit", await PrepareEditModelAsync(new InventoryEditInputModel()));
        }

        var inventory = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.Tags)
            .ThenInclude(x => x.Tag)
            .FirstOrDefaultAsync(x => x.Id == id.Value);

        if (inventory is null)
        {
            return NotFound();
        }

        if (!_permissions.CanEditInventory(inventory, User.GetUserId(), User.IsInRole("Admin")))
        {
            return Forbid();
        }

        return View("Edit", await PrepareEditModelAsync(new InventoryEditInputModel
        {
            Id = inventory.Id,
            Version = inventory.Version,
            Title = inventory.Title,
            Description = inventory.Description,
            Category = inventory.Category,
            ImageUrl = inventory.ImageUrl,
            IsPublicWrite = inventory.IsPublicWrite,
            Tags = string.Join(", ", inventory.Tags
                .OrderBy(x => x.Tag!.Name)
                .Select(x => x.Tag!.Name))
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(InventoryEditInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", await PrepareEditModelAsync(model));
        }

        if (!await CategoryExistsAsync(model.Category))
        {
            ModelState.AddModelError(nameof(model.Category), "Choose a category from the database lookup list.");
            return View("Edit", await PrepareEditModelAsync(model));
        }

        var userId = User.GetUserId()!;
        Inventory inventory;

        if (model.Id is { } id && id > 0)
        {
            inventory = await _db.Inventories
                .Include(x => x.Tags)
                .FirstAsync(x => x.Id == id);

            if (!_permissions.CanEditInventory(inventory, userId, User.IsInRole("Admin")))
            {
                return Forbid();
            }

            if (inventory.Version != model.Version)
            {
                ModelState.AddModelError(string.Empty, "This inventory was updated by another user. Reload and try again.");
                return View("Edit", await PrepareEditModelAsync(model));
            }
        }
        else
        {
            inventory = new Inventory
            {
                OwnerId = userId
            };
            _db.Inventories.Add(inventory);
        }

        await ApplyInventorySettingsAsync(inventory, model);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "This inventory was updated by another user. Reload and try again.");
            return View("Edit", await PrepareEditModelAsync(model));
        }

        return RedirectToAction(nameof(Details), new { id = inventory.Id, tab = "settings" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutosaveSettings(InventoryEditInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                message = "Inventory settings are invalid.",
                errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).Where(x => !string.IsNullOrWhiteSpace(x))
            });
        }

        if (!await CategoryExistsAsync(model.Category))
        {
            return BadRequest(new { message = "Choose a category from the database lookup list." });
        }

        if (model.Id is null or <= 0)
        {
            return BadRequest(new { message = "Autosave is available only for existing inventories." });
        }

        var inventory = await _db.Inventories
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Id == model.Id.Value);

        if (inventory is null)
        {
            return NotFound();
        }

        if (!_permissions.CanEditInventory(inventory, User.GetUserId(), User.IsInRole("Admin")))
        {
            return Forbid();
        }

        if (inventory.Version != model.Version)
        {
            return Conflict(new
            {
                message = "This inventory was updated by another user. Reload and try again.",
                currentVersion = inventory.Version
            });
        }

        await ApplyInventorySettingsAsync(inventory, model);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message = "This inventory was updated by another user. Reload and try again.",
                currentVersion = inventory.Version
            });
        }

        return Json(new
        {
            version = inventory.Version,
            updatedAt = inventory.UpdatedAt.ToLocalTime().ToString("g")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFieldDefinitions(int id, List<InventoryFieldDefinitionViewModel> fields)
    {
        var inventory = await _db.Inventories.Include(x => x.FieldDefinitions).FirstOrDefaultAsync(x => x.Id == id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!_permissions.CanEditInventory(inventory, User.GetUserId(), User.IsInRole("Admin")))
        {
            return Forbid();
        }

        ValidateFieldDefinitions(fields);
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Details), new { id, tab = "fields" });
        }

        var incomingIds = fields.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
        var removed = inventory.FieldDefinitions.Where(x => !incomingIds.Contains(x.Id)).ToList();
        _db.InventoryFieldDefinitions.RemoveRange(removed);

        foreach (var field in fields.OrderBy(x => x.SortOrder))
        {
            var definition = field.Id > 0
                ? inventory.FieldDefinitions.FirstOrDefault(x => x.Id == field.Id)
                : null;

            if (definition is null)
            {
                definition = new InventoryFieldDefinition();
                inventory.FieldDefinitions.Add(definition);
            }

            definition.FieldType = field.FieldType;
            definition.Title = field.Title;
            definition.Description = field.Description;
            definition.ShowInTable = field.ShowInTable;
            definition.IsRequired = field.IsRequired;
            definition.SortOrder = field.SortOrder;
            definition.RegexPattern = field.RegexPattern;
            definition.MinValue = field.MinValue;
            definition.MaxValue = field.MaxValue;
            definition.MaxLength = field.MaxLength;
            definition.OptionsJson = field.OptionsJson;
        }

        inventory.Version += 1;
        inventory.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id, tab = "fields" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCustomIdElements(int id, List<InventoryCustomIdElementViewModel> elements)
    {
        var inventory = await _db.Inventories.Include(x => x.CustomIdElements).FirstOrDefaultAsync(x => x.Id == id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!_permissions.CanEditInventory(inventory, User.GetUserId(), User.IsInRole("Admin")))
        {
            return Forbid();
        }

        if (elements.Count > 10)
        {
            ModelState.AddModelError(string.Empty, "Use no more than 10 custom ID elements.");
            return RedirectToAction(nameof(Details), new { id, tab = "customid" });
        }

        inventory.CustomIdElements.Clear();
        foreach (var element in elements.OrderBy(x => x.SortOrder))
        {
            inventory.CustomIdElements.Add(new InventoryCustomIdElement
            {
                ElementType = element.ElementType,
                FixedText = element.FixedText,
                Format = element.Format,
                SortOrder = element.SortOrder
            });
        }

        inventory.Version += 1;
        inventory.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id, tab = "customid" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAccess(int id, string[]? userIds)
    {
        var inventory = await _db.Inventories.Include(x => x.AccessGrants).FirstOrDefaultAsync(x => x.Id == id);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!_permissions.CanEditInventory(inventory, User.GetUserId(), User.IsInRole("Admin")))
        {
            return Forbid();
        }

        inventory.AccessGrants.Clear();
        foreach (var userId in (userIds ?? []).Distinct())
        {
            inventory.AccessGrants.Add(new InventoryAccessGrant { UserId = userId, InventoryId = id });
        }

        inventory.Version += 1;
        inventory.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id, tab = "access" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOwned(string[] selectedInventories)
    {
        var userId = User.GetUserId()!;
        var isAdmin = User.IsInRole("Admin");
        var requested = ParseVersionedKeys(selectedInventories);
        var inventoryIds = requested.Keys.ToArray();
        var inventories = await _db.Inventories
            .Where(x => inventoryIds.Contains(x.Id))
            .ToListAsync();

        var deletable = inventories.Where(x => _permissions.CanEditInventory(x, userId, isAdmin)).ToList();
        var stale = deletable.Where(x => requested.TryGetValue(x.Id, out var version) && x.Version != version).ToList();
        if (stale.Count > 0)
        {
            TempData["StatusMessage"] = "Some selected inventories changed after you loaded the page. Reload and review before deleting.";
            return RedirectToAction("Index", "Profile");
        }

        if (deletable.Count > 0)
        {
            _db.Inventories.RemoveRange(deletable);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["StatusMessage"] = "Some selected inventories changed during deletion. Reload and try again.";
            }
        }

        return RedirectToAction("Index", "Profile");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostDiscussion(int id, string markdown)
    {
        var inventory = await _db.Inventories
            .Include(x => x.AccessGrants)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (inventory is null)
        {
            return NotFound();
        }

        var canEditItems = _permissions.CanEditItems(inventory, User.GetUserId(), User.IsInRole("Admin"));
        if (!canEditItems)
        {
            return Forbid();
        }

        _db.InventoryDiscussionPosts.Add(new InventoryDiscussionPost
        {
            InventoryId = id,
            UserId = User.GetUserId()!,
            Markdown = markdown
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id, tab = "discussion" });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> DiscussionStream(int id)
    {
        var inventory = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.DiscussionPosts)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inventory is null)
        {
            return NotFound();
        }

        var posts = inventory.DiscussionPosts
            .OrderBy(x => x.CreatedAt)
            .Select(MapDiscussionPost)
            .Select(x => new
            {
                id = x.Id,
                authorId = x.AuthorId,
                authorName = x.AuthorName,
                html = x.Html,
                createdAt = x.CreatedAt.ToLocalTime().ToString("g")
            });

        return Json(posts);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ExportCsv(int id)
    {
        var inventory = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.FieldDefinitions)
            .Include(x => x.Items).ThenInclude(x => x.FieldValues)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inventory is null)
        {
            return NotFound();
        }

        var fields = inventory.FieldDefinitions.OrderBy(x => x.SortOrder).ToList();
        var lines = new List<string>
        {
            string.Join(",", new[] { "CustomId", "DisplayName", "CreatedAt" }.Concat(fields.Select(x => EscapeCsv(x.Title))))
        };

        lines.AddRange(inventory.Items.Select(item =>
            string.Join(",", new[]
            {
                EscapeCsv(item.CustomId),
                EscapeCsv(item.DisplayName),
                EscapeCsv(item.CreatedAt.ToString("u"))
            }.Concat(fields.Select(field => EscapeCsv(FormatField(item.FieldValues.FirstOrDefault(v => v.FieldDefinitionId == field.Id))))))));

        return File(System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines)), "text/csv", $"{inventory.Title}-export.csv");
    }

    [HttpGet]
    public async Task<IActionResult> UserLookup(string q, string sort = "name")
    {
        q = (q ?? string.Empty).Trim().ToLowerInvariant();
        var query = _db.Users.AsNoTracking().Where(x =>
            x.DisplayName.ToLower().Contains(q) || (x.Email ?? string.Empty).ToLower().Contains(q));
        query = sort == "email" ? query.OrderBy(x => x.Email) : query.OrderBy(x => x.DisplayName);

        var results = await query.Take(10).Select(x => new
        {
            x.Id,
            name = x.DisplayName,
            email = x.Email
        }).ToListAsync();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> TagLookup(string q)
    {
        q = (q ?? string.Empty).Trim().ToLowerInvariant();
        if (q.Length < 1)
        {
            return Json(Array.Empty<object>());
        }

        var results = await _db.Tags
            .AsNoTracking()
            .Where(x => x.Name.StartsWith(q))
            .OrderBy(x => x.Name)
            .Take(10)
            .Select(x => new { name = x.Name })
            .ToListAsync();

        return Json(results);
    }

    private async Task ApplyInventorySettingsAsync(Inventory inventory, InventoryEditInputModel model)
    {
        inventory.Title = model.Title.Trim();
        inventory.Description = model.Description.Trim();
        inventory.Category = model.Category.Trim();
        inventory.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();
        inventory.IsPublicWrite = model.IsPublicWrite;
        inventory.UpdatedAt = DateTimeOffset.UtcNow;
        inventory.Version += 1;

        inventory.Tags.Clear();
        var normalizedTagNames = model.Tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var existingTags = await _db.Tags
            .Where(x => normalizedTagNames.Contains(x.Name))
            .ToDictionaryAsync(x => x.Name);

        foreach (var normalized in normalizedTagNames)
        {
            if (!existingTags.TryGetValue(normalized, out var tag))
            {
                tag = new Tag { Name = normalized };
                existingTags[normalized] = tag;
            }

            inventory.Tags.Add(new InventoryTag { Inventory = inventory, Tag = tag });
        }
    }

    private async Task<InventoryEditInputModel> PrepareEditModelAsync(InventoryEditInputModel model)
    {
        model.Categories = await GetCategoriesAsync();
        model.CloudinaryCloudName = _configuration["Cloudinary:CloudName"];
        model.CloudinaryUploadPreset = _configuration["Cloudinary:UploadPreset"];
        return model;
    }

    private async Task<List<string>> GetCategoriesAsync() =>
        await _db.InventoryCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => x.Name)
            .ToListAsync();

    private async Task<bool> CategoryExistsAsync(string category) =>
        await _db.InventoryCategories.AnyAsync(x => x.Name == category.Trim());

    private static Dictionary<int, int> ParseVersionedKeys(IEnumerable<string> keys)
    {
        var parsed = new Dictionary<int, int>();
        foreach (var key in keys)
        {
            var parts = key.Split(':', 2);
            if (parts.Length == 2
                && int.TryParse(parts[0], out var id)
                && int.TryParse(parts[1], out var version))
            {
                parsed[id] = version;
            }
        }

        return parsed;
    }

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private InventoryDiscussionPostViewModel MapDiscussionPost(InventoryDiscussionPost post) =>
        new()
        {
            Id = post.Id,
            AuthorId = post.UserId,
            AuthorName = post.User?.DisplayName ?? post.User?.Email ?? "Unknown",
            Markdown = post.Markdown,
            Html = _markdownRenderer.Render(post.Markdown),
            CreatedAt = post.CreatedAt
        };

    private void ValidateFieldDefinitions(List<InventoryFieldDefinitionViewModel> fields)
    {
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Title))
            {
                ModelState.AddModelError(string.Empty, "Each field needs a title.");
            }

            if (field.FieldType == InventoryFieldTypes.Select && !string.IsNullOrWhiteSpace(field.OptionsJson))
            {
                try
                {
                    JsonSerializer.Deserialize<List<string>>(field.OptionsJson);
                }
                catch (JsonException)
                {
                    ModelState.AddModelError(string.Empty, $"Field '{field.Title}' has invalid select options JSON.");
                }
            }
        }
    }

    private static string FormatField(ItemFieldValue? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value.NumericValue.HasValue)
        {
            return value.NumericValue.Value.ToString("0.##");
        }

        if (value.BoolValue.HasValue)
        {
            return value.BoolValue.Value ? "Yes" : "No";
        }

        return value.StringValue ?? string.Empty;
    }

    private static InventoryStatisticsViewModel BuildStatistics(Inventory inventory)
    {
        var numericFields = inventory.FieldDefinitions
            .Where(x => x.FieldType == InventoryFieldTypes.Number)
            .Select(field =>
            {
                var values = inventory.Items
                    .SelectMany(item => item.FieldValues)
                    .Where(v => v.FieldDefinitionId == field.Id && v.NumericValue.HasValue)
                    .Select(v => v.NumericValue!.Value)
                    .ToList();

                return new NumericFieldStatsViewModel
                {
                    Title = field.Title,
                    Average = values.Count == 0 ? null : values.Average(),
                    Min = values.Count == 0 ? null : values.Min(),
                    Max = values.Count == 0 ? null : values.Max()
                };
            }).ToList();

        var stringFields = inventory.FieldDefinitions
            .Where(x => x.FieldType is InventoryFieldTypes.SingleLineText or InventoryFieldTypes.MultiLineText or InventoryFieldTypes.Select)
            .Select(field =>
            {
                var value = inventory.Items
                    .SelectMany(item => item.FieldValues)
                    .Where(v => v.FieldDefinitionId == field.Id && !string.IsNullOrWhiteSpace(v.StringValue))
                    .GroupBy(v => v.StringValue)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                return new TopStringFieldStatsViewModel
                {
                    Title = field.Title,
                    MostFrequentValue = value
                };
            }).ToList();

        return new InventoryStatisticsViewModel
        {
            ItemCount = inventory.Items.Count,
            NumericFields = numericFields,
            StringFields = stringFields
        };
    }
}
