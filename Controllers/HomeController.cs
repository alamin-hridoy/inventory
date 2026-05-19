using System.Diagnostics;
using InventoryPilot.Data;
using Microsoft.AspNetCore.Mvc;
using InventoryPilot.Models;
using InventoryPilot.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InventoryPilot.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var latest = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Tags).ThenInclude(x => x.Tag)
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .ToListAsync();

        var popular = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Tags).ThenInclude(x => x.Tag)
            .Include(x => x.Items)
            .OrderByDescending(x => x.Items.Count)
            .Take(5)
            .ToListAsync();

        var tags = await _db.Tags
            .AsNoTracking()
            .Select(x => new TagCloudEntryViewModel
            {
                Name = x.Name,
                Count = x.Inventories.Count
            })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync();

        var model = new HomeIndexViewModel
        {
            LatestInventories = latest.Select(MapSummary).ToList(),
            PopularInventories = popular.Select(MapSummary).ToList(),
            TagCloud = tags
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q, string? tag = null)
    {
        q = (q ?? tag ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(q))
        {
            return View(new SearchResultViewModel());
        }

        const string searchConfig = "english";

        var inventories = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Tags).ThenInclude(x => x.Tag)
            .Include(x => x.Items)
            .Where(x => EF.Functions.ToTsVector(searchConfig, x.Title + " " + x.Description)
                            .Matches(EF.Functions.PlainToTsQuery(searchConfig, q))
                        || x.Tags.Any(t => EF.Functions.ToTsVector(searchConfig, t.Tag!.Name)
                            .Matches(EF.Functions.PlainToTsQuery(searchConfig, q))))
            .OrderByDescending(x => x.UpdatedAt)
            .Take(20)
            .ToListAsync();

        var items = await _db.ItemRecords
            .AsNoTracking()
            .Include(x => x.Inventory)
            .Where(x => EF.Functions.ToTsVector(searchConfig, x.CustomId + " " + x.DisplayName)
                            .Matches(EF.Functions.PlainToTsQuery(searchConfig, q))
                        || x.FieldValues.Any(v => v.StringValue != null
                            && EF.Functions.ToTsVector(searchConfig, v.StringValue)
                                .Matches(EF.Functions.PlainToTsQuery(searchConfig, q))))
            .OrderByDescending(x => x.UpdatedAt)
            .Take(50)
            .Select(x => new ItemSearchHitViewModel
            {
                ItemId = x.Id,
                InventoryId = x.InventoryId,
                InventoryTitle = x.Inventory!.Title,
                CustomId = x.CustomId,
                DisplayName = x.DisplayName
            })
            .ToListAsync();

        return View(new SearchResultViewModel
        {
            Query = q,
            Inventories = inventories.Select(MapSummary).ToList(),
            Items = items
        });
    }

    private static InventorySummaryViewModel MapSummary(Inventory inventory) =>
        new()
        {
            Id = inventory.Id,
            Version = inventory.Version,
            Title = inventory.Title,
            Description = inventory.Description,
            Category = inventory.Category,
            OwnerName = inventory.Owner?.DisplayName ?? inventory.Owner?.Email ?? "Unknown",
            ImageUrl = inventory.ImageUrl,
            ItemCount = inventory.Items.Count,
            Tags = inventory.Tags.Select(x => x.Tag?.Name ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
        };
}
