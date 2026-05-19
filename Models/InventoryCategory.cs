using System.ComponentModel.DataAnnotations;

namespace InventoryPilot.Models;

public class InventoryCategory
{
    public int Id { get; set; }

    [Required, StringLength(64)]
    public string Name { get; set; } = string.Empty;
}
