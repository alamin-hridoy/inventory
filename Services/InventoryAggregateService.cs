using InventoryPilot.Data;
using InventoryPilot.Models;
using InventoryPilot.Models.Integrations;
using Microsoft.EntityFrameworkCore;

namespace InventoryPilot.Services;

public class InventoryAggregateService
{
    private readonly ApplicationDbContext _db;

    public InventoryAggregateService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<InventoryAggregateResult?> BuildAsync(int inventoryId)
    {
        var inventory = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.FieldDefinitions)
            .Include(x => x.Items).ThenInclude(x => x.FieldValues)
            .FirstOrDefaultAsync(x => x.Id == inventoryId);

        if (inventory is null)
        {
            return null;
        }

        var fields = inventory.FieldDefinitions
            .OrderBy(x => x.SortOrder)
            .Select(field => BuildFieldAggregate(field, inventory.Items))
            .ToList();

        return new InventoryAggregateResult
        {
            InventoryId = inventory.Id,
            InventoryTitle = inventory.Title,
            Category = inventory.Category,
            ItemCount = inventory.Items.Count,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Fields = fields
        };
    }

    private static InventoryFieldAggregate BuildFieldAggregate(InventoryFieldDefinition field, IEnumerable<ItemRecord> items)
    {
        var values = items
            .SelectMany(x => x.FieldValues)
            .Where(x => x.FieldDefinitionId == field.Id)
            .ToList();

        var aggregate = new InventoryFieldAggregate
        {
            FieldId = field.Id,
            Title = field.Title,
            Type = field.FieldType
        };

        if (field.FieldType == InventoryFieldTypes.Number)
        {
            var numericValues = values
                .Where(x => x.NumericValue.HasValue)
                .Select(x => x.NumericValue!.Value)
                .ToList();

            if (numericValues.Count > 0)
            {
                aggregate.Average = numericValues.Average();
                aggregate.Min = numericValues.Min();
                aggregate.Max = numericValues.Max();
            }
        }
        else if (field.FieldType is InventoryFieldTypes.SingleLineText or InventoryFieldTypes.MultiLineText or InventoryFieldTypes.Link or InventoryFieldTypes.Select)
        {
            aggregate.TopValues = values
                .Select(x => x.StringValue)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x!)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Key)
                .Take(5)
                .Select(x => new InventoryTopValue(x.Key, x.Count()))
                .ToList();
        }
        else if (field.FieldType == InventoryFieldTypes.Boolean)
        {
            aggregate.TopValues = values
                .Where(x => x.BoolValue.HasValue)
                .GroupBy(x => x.BoolValue!.Value ? "true" : "false")
                .OrderByDescending(x => x.Count())
                .Select(x => new InventoryTopValue(x.Key, x.Count()))
                .ToList();
        }

        return aggregate;
    }
}
