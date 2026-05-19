using InventoryPilot.Data;
using InventoryPilot.Extensions;
using InventoryPilot.Models;
using InventoryPilot.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryPilot.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var adminRoleId = await _db.Roles
            .Where(x => x.Name == "Admin")
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        var adminUserIds = string.IsNullOrWhiteSpace(adminRoleId)
            ? new HashSet<string>()
            : (await _db.UserRoles
                .Where(x => x.RoleId == adminRoleId)
                .Select(x => x.UserId)
                .ToListAsync())
            .ToHashSet();
        var users = await _userManager.Users.OrderBy(x => x.DisplayName).ToListAsync();
        var model = users.Select(user => new UserAdminRowViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                IsBlocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                IsAdmin = adminUserIds.Contains(user.Id)
            })
            .ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlock(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
            ? null
            : DateTimeOffset.UtcNow.AddYears(10);
        await _userManager.UpdateAsync(user);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkBlock(string[] userIds, bool block)
    {
        var users = await _userManager.Users.Where(x => userIds.Contains(x.Id)).ToListAsync();
        foreach (var user in users)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = block ? DateTimeOffset.UtcNow.AddYears(10) : null;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Admin");
        }
        else
        {
            await _userManager.AddToRoleAsync(user, "Admin");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAdmin(string[] userIds, bool makeAdmin)
    {
        var adminRole = await _db.Roles.FirstAsync(x => x.Name == "Admin");
        var selectedUserIds = userIds.Distinct().ToList();
        var existingAdminIds = await _db.UserRoles
            .Where(x => x.RoleId == adminRole.Id && selectedUserIds.Contains(x.UserId))
            .Select(x => x.UserId)
            .ToListAsync();

        if (makeAdmin)
        {
            var newAdminRoles = selectedUserIds
                .Except(existingAdminIds)
                .Select(userId => new IdentityUserRole<string> { UserId = userId, RoleId = adminRole.Id });
            _db.UserRoles.AddRange(newAdminRoles);
        }
        else
        {
            var rolesToRemove = await _db.UserRoles
                .Where(x => x.RoleId == adminRole.Id && selectedUserIds.Contains(x.UserId))
                .ToListAsync();
            _db.UserRoles.RemoveRange(rolesToRemove);
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUsers(string[] userIds)
    {
        var selectedUserIds = userIds.Distinct().ToList();
        var replacementOwnerId = User.GetUserId();
        if (replacementOwnerId is null || selectedUserIds.Contains(replacementOwnerId))
        {
            replacementOwnerId = await _db.Users
                .Where(x => !selectedUserIds.Contains(x.Id))
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
        }

        if (replacementOwnerId is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var ownedInventories = await _db.Inventories
            .Where(x => selectedUserIds.Contains(x.OwnerId))
            .ToListAsync();
        foreach (var inventory in ownedInventories)
        {
            inventory.OwnerId = replacementOwnerId;
        }

        var createdItems = await _db.ItemRecords
            .Where(x => selectedUserIds.Contains(x.CreatedById))
            .ToListAsync();
        foreach (var item in createdItems)
        {
            item.CreatedById = replacementOwnerId;
        }

        _db.InventoryAccessGrants.RemoveRange(_db.InventoryAccessGrants.Where(x => selectedUserIds.Contains(x.UserId)));
        _db.InventoryDiscussionPosts.RemoveRange(_db.InventoryDiscussionPosts.Where(x => selectedUserIds.Contains(x.UserId)));
        _db.ItemLikes.RemoveRange(_db.ItemLikes.Where(x => selectedUserIds.Contains(x.UserId)));
        await _db.SaveChangesAsync();

        var users = await _db.Users.Where(x => selectedUserIds.Contains(x.Id)).ToListAsync();
        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
