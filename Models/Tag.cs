using System.ComponentModel.DataAnnotations;

namespace InventoryPilot.Models;

public class Tag
{
    public int Id { get; set; }

    [Required, StringLength(64)]
    public string Name { get; set; } = string.Empty;

    public ICollection<InventoryTag> Inventories { get; set; } = new List<InventoryTag>();
}
