using System.ComponentModel.DataAnnotations;

namespace AIMS.WebPortal.Models;

public class KanbanStatusUpdateRequest
{
    [Required]
    public int Id { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }
}
