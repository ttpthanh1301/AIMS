namespace AIMS.ViewModels.TaskManagement;

public class TaskDetailVm
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
    public List<TaskActivityVm> Activities { get; set; } = new();
}

public class TaskActivityVm
{
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
