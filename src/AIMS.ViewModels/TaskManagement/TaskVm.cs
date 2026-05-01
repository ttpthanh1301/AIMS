using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.TaskManagement;

public class TaskVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int AssignmentId { get; set; }
    public string InternName { get; set; } = string.Empty;
    public string Priority { get; set; } = "MEDIUM";
    public string Status { get; set; } = "TODO";
    public DateTime Deadline { get; set; }
    public decimal? EstimatedHours { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}

public class CreateTaskRequest
{
    [Required(ErrorMessage = "Tiêu đề task không được để trống")]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public int AssignmentId { get; set; }

    public string Priority { get; set; } = "MEDIUM";
    // LOW / MEDIUM / HIGH

    [Required]
    public DateTime Deadline { get; set; }

    public decimal? EstimatedHours { get; set; }
}

public class UpdateTaskRequest
{
    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public string Priority { get; set; } = "MEDIUM";
    public DateTime Deadline { get; set; }
    public decimal? EstimatedHours { get; set; }
}

public class UpdateTaskStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
    // TODO / IN_PROGRESS / DONE / OVERDUE

    [StringLength(500)]
    public string? Note { get; set; }
}
