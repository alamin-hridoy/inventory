using System.ComponentModel.DataAnnotations;

namespace InventoryPilot.Models.ViewModels;

public class SupportTicketInputModel
{
    [Required, StringLength(160)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = "Average";

    public int? InventoryId { get; set; }
    public string? InventoryTitle { get; set; }
    public string? ReturnUrl { get; set; }

    [Required, StringLength(1000)]
    public string AdminEmails { get; set; } = string.Empty;
}
