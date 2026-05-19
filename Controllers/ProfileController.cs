using InventoryPilot.Data;
using InventoryPilot.Extensions;
using InventoryPilot.Models.ViewModels;
using InventoryPilot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryPilot.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;

    public ProfileController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId()!;
        var model = await BuildProfileAsync(userId);
        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Public(string id)
    {
        var model = await BuildProfileAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        model = new ProfileViewModel
        {
            DisplayName = model.DisplayName,
            OwnedInventories = model.OwnedInventories,
            WritableInventories = []
        };

        ViewData["Title"] = model.DisplayName;
        return View("Public", model);
    }

    private async Task<ProfileViewModel?> BuildProfileAsync(string userId)
    {
        var owner = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (owner is null)
        {
            return null;
        }

        var owned = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Tags).ThenInclude(x => x.Tag)
            .Include(x => x.Items)
            .Where(x => x.OwnerId == userId)
            .OrderBy(x => x.Title)
            .ToListAsync();

        var writable = await _db.Inventories
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Tags).ThenInclude(x => x.Tag)
            .Include(x => x.Items)
            .Include(x => x.AccessGrants)
            .Where(x => x.IsPublicWrite || x.AccessGrants.Any(g => g.UserId == userId))
            .OrderBy(x => x.Title)
            .ToListAsync();

        return new ProfileViewModel
        {
            DisplayName = owner.DisplayName,
            OwnedInventories = owned.Select(x => new InventorySummaryViewModel
            {
                Id = x.Id,
                Version = x.Version,
                Title = x.Title,
                Description = x.Description,
                Category = x.Category,
                OwnerName = x.Owner!.DisplayName,
                ItemCount = x.Items.Count,
                ImageUrl = x.ImageUrl,
                Tags = x.Tags.Select(t => t.Tag!.Name).ToList()
            }).ToList(),
            WritableInventories = writable.Select(x => new InventorySummaryViewModel
            {
                Id = x.Id,
                Version = x.Version,
                Title = x.Title,
                Description = x.Description,
                Category = x.Category,
                OwnerName = x.Owner!.DisplayName,
                ItemCount = x.Items.Count,
                ImageUrl = x.ImageUrl,
                Tags = x.Tags.Select(t => t.Tag!.Name).ToList()
            }).ToList()
        };
    }
}
