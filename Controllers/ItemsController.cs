using InventoryPilot.Data;
using InventoryPilot.Extensions;
using InventoryPilot.Models;
using InventoryPilot.Models.ViewModels;
using InventoryPilot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InventoryPilot.Controllers;

[Authorize]
public class ItemsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly InventoryPermissionService _permissions;
    private readonly CustomIdComposer _customIdComposer;

    public ItemsController(ApplicationDbContext db, InventoryPermissionService permissions, CustomIdComposer customIdComposer)
    {
        _db = db;
        _permissions = permissions;
        _customIdComposer = customIdComposer;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.ItemRecords
            .AsNoTracking()
            .Include(x => x.Inventory).ThenInclude(x => x!.FieldDefinitions)
            .Include(x => x.FieldValues).ThenInclude(x => x.FieldDefinition)
            .Include(x => x.Likes)
            .Include(x => x.CreatedBy)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        var model = new ItemEditViewModel
        {
            Id = item.Id,
            InventoryId = item.InventoryId,
            Version = item.Version,
            CreatedByName = item.CreatedBy?.DisplayName ?? item.CreatedBy?.Email,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            CustomId = item.CustomId,
            DisplayName = item.DisplayName,
            Fields = item.Inventory!.FieldDefinitions.OrderBy(x => x.SortOrder).Select(field =>
            {
                var value = item.FieldValues.FirstOrDefault(v => v.FieldDefinitionId == field.Id);
                return new ItemFieldInputModel
                {
                    FieldDefinitionId = field.Id,
                    FieldType = field.FieldType,
                    Title = field.Title,
                    Description = field.Description,
                    IsRequired = field.IsRequired,
                    StringValue = value?.StringValue,
                    NumericValue = value?.NumericValue,
                    BoolValue = value?.BoolValue,
                    OptionsJson = field.OptionsJson
                };
            }).ToList()
        };

        ViewBag.InventoryTitle = item.Inventory.Title;
        ViewBag.LikeCount = item.Likes.Count;
        ViewBag.CanEdit = _permissions.CanEditItems(item.Inventory, User.GetUserId(), User.IsInRole("Admin"));
        return View("Edit", model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int inventoryId)
    {
        var inventory = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.AccessGrants)
            .Include(x => x.CustomIdElements)
            .Include(x => x.FieldDefinitions)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == inventoryId);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!_permissions.CanEditItems(inventory, User.GetUserId(), User.IsInRole("Admin")))
        {
            return Forbid();
        }

        var model = new ItemEditViewModel
        {
            InventoryId = inventoryId,
            CreatedByName = User.Identity?.Name,
            CustomId = _customIdComposer.Generate(inventory.CustomIdElements, NextSequence(inventory.Items)),
            Fields = inventory.FieldDefinitions.OrderBy(x => x.SortOrder).Select(x => new ItemFieldInputModel
            {
                FieldDefinitionId = x.Id,
                FieldType = x.FieldType,
                Title = x.Title,
                Description = x.Description,
                IsRequired = x.IsRequired,
                OptionsJson = x.OptionsJson
            }).ToList()
        };

        ViewBag.InventoryTitle = inventory.Title;
        ViewBag.CanEdit = true;
        return View("Edit", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ItemEditViewModel model)
    {
        var inventory = await _db.Inventories
            .Include(x => x.AccessGrants)
            .Include(x => x.CustomIdElements)
            .Include(x => x.FieldDefinitions)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == model.InventoryId);
        if (inventory is null)
        {
            return NotFound();
        }

        if (!_permissions.CanEditItems(inventory, User.GetUserId(), User.IsInRole("Admin")))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.InventoryTitle = inventory.Title;
            ViewBag.CanEdit = true;
            return View("Edit", model);
        }

        ValidateItemModel(model, inventory);
        if (!ModelState.IsValid)
        {
            ViewBag.InventoryTitle = inventory.Title;
            ViewBag.CanEdit = true;
            return View("Edit", model);
        }

        ItemRecord item;
        if (model.Id is { } id && id > 0)
        {
            item = await _db.ItemRecords.Include(x => x.FieldValues).FirstAsync(x => x.Id == id);
            if (item.Version != model.Version)
            {
                ModelState.AddModelError(string.Empty, "This item was updated by another user. Reload and try again.");
                ViewBag.InventoryTitle = inventory.Title;
                ViewBag.CanEdit = true;
                return View("Edit", model);
            }
        }
        else
        {
            var nextSequence = ((await _db.ItemRecords
                .Where(x => x.InventoryId == inventory.Id)
                .MaxAsync(x => (int?)x.SequenceNumber)) ?? 0) + 1;
            item = new ItemRecord
            {
                InventoryId = inventory.Id,
                SequenceNumber = nextSequence,
                CreatedById = User.GetUserId()!
            };
            _db.ItemRecords.Add(item);
        }

        item.CustomId = model.CustomId.Trim();
        item.DisplayName = model.DisplayName.Trim();
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.Version += 1;

        foreach (var fieldInput in model.Fields)
        {
            var fieldValue = item.FieldValues.FirstOrDefault(x => x.FieldDefinitionId == fieldInput.FieldDefinitionId);
            if (fieldValue is null)
            {
                fieldValue = new ItemFieldValue { FieldDefinitionId = fieldInput.FieldDefinitionId };
                item.FieldValues.Add(fieldValue);
            }

            fieldValue.StringValue = fieldInput.StringValue;
            fieldValue.NumericValue = fieldInput.NumericValue;
            fieldValue.BoolValue = fieldInput.BoolValue;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "This item was updated by another user. Reload and try again.");
            ViewBag.InventoryTitle = inventory.Title;
            ViewBag.CanEdit = true;
            return View("Edit", model);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.CustomId), "This custom ID is already used in the inventory or does not match the inventory rules. Edit it and try again.");
            ViewBag.InventoryTitle = inventory.Title;
            ViewBag.CanEdit = true;
            return View("Edit", model);
        }

        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelected(int inventoryId, string[] selectedItems)
    {
        var inventory = await _db.Inventories.Include(x => x.AccessGrants).FirstAsync(x => x.Id == inventoryId);
        if (!_permissions.CanEditItems(inventory, User.GetUserId(), User.IsInRole("Admin")))
        {
            return Forbid();
        }

        var requested = ParseVersionedKeys(selectedItems);
        var itemIds = requested.Keys.ToArray();
        var items = await _db.ItemRecords.Where(x => x.InventoryId == inventoryId && itemIds.Contains(x.Id)).ToListAsync();
        var stale = items.Where(x => requested.TryGetValue(x.Id, out var version) && x.Version != version).ToList();
        if (stale.Count > 0)
        {
            TempData["StatusMessage"] = "Some selected items changed after you loaded the page. Reload and review before deleting.";
            return RedirectToAction("Details", "Inventories", new { id = inventoryId, tab = "items" });
        }

        _db.ItemRecords.RemoveRange(items);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["StatusMessage"] = "Some selected items changed during deletion. Reload and try again.";
        }
        return RedirectToAction("Details", "Inventories", new { id = inventoryId, tab = "items" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var userId = User.GetUserId()!;
        var existing = await _db.ItemLikes.FirstOrDefaultAsync(x => x.ItemRecordId == id && x.UserId == userId);
        if (existing is null)
        {
            _db.ItemLikes.Add(new ItemLike { ItemRecordId = id, UserId = userId });
        }
        else
        {
            _db.ItemLikes.Remove(existing);
        }

        await _db.SaveChangesAsync();
        var item = await _db.ItemRecords.AsNoTracking().FirstAsync(x => x.Id == id);
        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

    private void ValidateItemModel(ItemEditViewModel model, Inventory inventory)
    {
            if (string.IsNullOrWhiteSpace(model.CustomId))
        {
            ModelState.AddModelError(nameof(model.CustomId), "Custom ID is required.");
        }
        else if (!_customIdComposer.IsValid(model.CustomId.Trim(), inventory.CustomIdElements))
        {
            ModelState.AddModelError(nameof(model.CustomId), "Custom ID does not match the current inventory format.");
        }

        if (string.IsNullOrWhiteSpace(model.DisplayName))
        {
            ModelState.AddModelError(nameof(model.DisplayName), "Item name is required.");
        }

        foreach (var fieldInput in model.Fields)
        {
            var definition = inventory.FieldDefinitions.FirstOrDefault(x => x.Id == fieldInput.FieldDefinitionId);
            if (definition is null)
            {
                continue;
            }

            if (definition.IsRequired)
            {
                var hasValue = definition.FieldType switch
                {
                    InventoryFieldTypes.Number => fieldInput.NumericValue.HasValue,
                    InventoryFieldTypes.Boolean => fieldInput.BoolValue.HasValue,
                    _ => !string.IsNullOrWhiteSpace(fieldInput.StringValue)
                };

                if (!hasValue)
                {
                    ModelState.AddModelError(string.Empty, $"'{definition.Title}' is required.");
                }
            }

            if (definition.FieldType is InventoryFieldTypes.SingleLineText or InventoryFieldTypes.MultiLineText or InventoryFieldTypes.Link or InventoryFieldTypes.Select)
            {
                var stringValue = fieldInput.StringValue?.Trim();
                fieldInput.StringValue = stringValue;

                if (definition.MaxLength.HasValue && !string.IsNullOrWhiteSpace(stringValue) && stringValue.Length > definition.MaxLength.Value)
                {
                    ModelState.AddModelError(string.Empty, $"'{definition.Title}' exceeds the maximum length.");
                }

                if (!string.IsNullOrWhiteSpace(definition.RegexPattern) && !string.IsNullOrWhiteSpace(stringValue) && !Regex.IsMatch(stringValue, definition.RegexPattern))
                {
                    ModelState.AddModelError(string.Empty, $"'{definition.Title}' does not match the required format.");
                }

                if (definition.FieldType == InventoryFieldTypes.Link
                    && !string.IsNullOrWhiteSpace(stringValue)
                    && !Uri.TryCreate(stringValue, UriKind.Absolute, out _))
                {
                    ModelState.AddModelError(string.Empty, $"'{definition.Title}' must be a valid absolute URL.");
                }

                if (definition.FieldType == InventoryFieldTypes.Select
                    && !string.IsNullOrWhiteSpace(stringValue)
                    && !string.IsNullOrWhiteSpace(definition.OptionsJson))
                {
                    var options = JsonSerializer.Deserialize<List<string>>(definition.OptionsJson) ?? [];
                    if (!options.Contains(stringValue))
                    {
                        ModelState.AddModelError(string.Empty, $"'{definition.Title}' has an invalid value.");
                    }
                }
            }

            if (definition.FieldType == InventoryFieldTypes.Number && fieldInput.NumericValue.HasValue)
            {
                if (definition.MinValue.HasValue && fieldInput.NumericValue.Value < definition.MinValue.Value)
                {
                    ModelState.AddModelError(string.Empty, $"'{definition.Title}' is below the minimum allowed value.");
                }

                if (definition.MaxValue.HasValue && fieldInput.NumericValue.Value > definition.MaxValue.Value)
                {
                    ModelState.AddModelError(string.Empty, $"'{definition.Title}' is above the maximum allowed value.");
                }
            }
        }
    }

    private static int NextSequence(IEnumerable<ItemRecord> items)
    {
        var max = items.Select(x => x.SequenceNumber).DefaultIfEmpty(0).Max();
        return max + 1;
    }

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
}
