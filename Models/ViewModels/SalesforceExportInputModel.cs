using System.ComponentModel.DataAnnotations;

namespace InventoryPilot.Models.ViewModels;

public class SalesforceExportInputModel
{
    public string? UserId { get; set; }

    [Required, StringLength(160)]
    public string AccountName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(120)]
    public string? Title { get; set; }

    [Url, StringLength(200)]
    public string? Website { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
