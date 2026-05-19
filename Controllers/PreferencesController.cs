using InventoryPilot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPilot.Controllers;

public class PreferencesController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public PreferencesController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTheme(string theme, string? returnUrl = null)
    {
        Response.Cookies.Append("theme", theme, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                user.PreferredTheme = theme;
                await _userManager.UpdateAsync(user);
            }
        }

        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLanguage(string language, string? returnUrl = null)
    {
        Response.Cookies.Append("language", language, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                user.PreferredLanguage = language;
                await _userManager.UpdateAsync(user);
            }
        }

        return LocalRedirect(returnUrl ?? "/");
    }
}
