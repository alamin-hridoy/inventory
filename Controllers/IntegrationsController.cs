using InventoryPilot.Data;
using InventoryPilot.Extensions;
using InventoryPilot.Models.ViewModels;
using InventoryPilot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryPilot.Controllers;

[Authorize]
public class IntegrationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<InventoryPilot.Models.ApplicationUser> _userManager;
    private readonly SalesforceService _salesforceService;

    public IntegrationsController(ApplicationDbContext db, UserManager<InventoryPilot.Models.ApplicationUser> userManager, SalesforceService salesforceService)
    {
        _db = db;
        _userManager = userManager;
        _salesforceService = salesforceService;
    }

    [HttpGet]
    public async Task<IActionResult> Salesforce(string? userId = null)
    {
        var targetUser = await GetTargetUserAsync(userId);
        if (targetUser is null)
        {
            return Forbid();
        }

        var names = SplitName(targetUser.DisplayName, targetUser.Email ?? string.Empty);
        return View(new SalesforceExportInputModel
        {
            UserId = targetUser.Id,
            AccountName = string.IsNullOrWhiteSpace(targetUser.DisplayName) ? targetUser.Email ?? string.Empty : targetUser.DisplayName,
            FirstName = names.FirstName,
            LastName = names.LastName,
            Email = targetUser.Email ?? string.Empty,
            Phone = targetUser.PhoneNumber
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salesforce(SalesforceExportInputModel model)
    {
        var targetUser = await GetTargetUserAsync(model.UserId);
        if (targetUser is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _salesforceService.CreateAccountAndContactAsync(model);
        TempData["StatusMessage"] = result.Message;
        if (!result.Success)
        {
            return View(model);
        }

        return RedirectToAction("Index", "Profile");
    }

    private async Task<InventoryPilot.Models.ApplicationUser?> GetTargetUserAsync(string? userId)
    {
        var currentUserId = User.GetUserId();
        var requestedUserId = string.IsNullOrWhiteSpace(userId) ? currentUserId : userId;
        if (requestedUserId != currentUserId && !User.IsInRole("Admin"))
        {
            return null;
        }

        return await _userManager.Users.FirstOrDefaultAsync(x => x.Id == requestedUserId);
    }

    private static (string FirstName, string LastName) SplitName(string displayName, string fallbackEmail)
    {
        var parts = (displayName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            var fallback = fallbackEmail.Split('@')[0];
            return (fallback, fallback);
        }

        if (parts.Length == 1)
        {
            return (parts[0], parts[0]);
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }
}
