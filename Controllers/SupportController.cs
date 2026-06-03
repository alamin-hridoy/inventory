using InventoryPilot.Data;
using InventoryPilot.Extensions;
using InventoryPilot.Models.Integrations;
using InventoryPilot.Models.ViewModels;
using InventoryPilot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryPilot.Controllers;

[Authorize]
public class SupportController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly SupportTicketUploadService _uploadService;
    private readonly IConfiguration _configuration;

    public SupportController(ApplicationDbContext db, SupportTicketUploadService uploadService, IConfiguration configuration)
    {
        _db = db;
        _uploadService = uploadService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? returnUrl = null, int? inventoryId = null)
    {
        var model = new SupportTicketInputModel
        {
            ReturnUrl = NormalizeReturnUrl(returnUrl),
            InventoryId = inventoryId,
            AdminEmails = await GetAdminEmailsAsync()
        };

        if (inventoryId.HasValue)
        {
            model.InventoryTitle = await _db.Inventories
                .AsNoTracking()
                .Where(x => x.Id == inventoryId.Value)
                .Select(x => x.Title)
                .FirstOrDefaultAsync();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupportTicketInputModel model)
    {
        model.ReturnUrl = NormalizeReturnUrl(model.ReturnUrl);
        if (!new[] { "High", "Average", "Low" }.Contains(model.Priority))
        {
            ModelState.AddModelError(nameof(model.Priority), "Choose High, Average, or Low.");
        }

        if (model.InventoryId.HasValue && string.IsNullOrWhiteSpace(model.InventoryTitle))
        {
            model.InventoryTitle = await _db.Inventories
                .AsNoTracking()
                .Where(x => x.Id == model.InventoryId.Value)
                .Select(x => x.Title)
                .FirstOrDefaultAsync();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var absoluteLink = BuildAbsoluteLink(model.ReturnUrl);
        var payload = new SupportTicketPayload
        {
            ReportedBy = User.Identity?.Name ?? User.GetUserId() ?? "Unknown",
            Inventory = model.InventoryTitle,
            Link = absoluteLink,
            Priority = model.Priority,
            Summary = model.Summary,
            AdminEmails = model.AdminEmails
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var result = await _uploadService.UploadAsync(payload);
        TempData["StatusMessage"] = result.Message;
        return result.Success ? Redirect(model.ReturnUrl ?? Url.Action("Index", "Home")!) : View(model);
    }

    private string? NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return Url.Action("Index", "Home");
        }

        return returnUrl;
    }

    private string BuildAbsoluteLink(string? localUrl)
    {
        localUrl = NormalizeReturnUrl(localUrl) ?? "/";
        return $"{Request.Scheme}://{Request.Host}{localUrl}";
    }

    private async Task<string> GetAdminEmailsAsync()
    {
        var configured = _configuration["SupportTickets:AdminEmails"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var adminEmails = await _db.UserRoles
            .Join(_db.Roles.Where(x => x.Name == "Admin"), userRole => userRole.RoleId, role => role.Id, (userRole, _) => userRole.UserId)
            .Join(_db.Users, userId => userId, user => user.Id, (_, user) => user.Email)
            .Where(email => email != null)
            .ToListAsync();

        return string.Join(", ", adminEmails);
    }
}
