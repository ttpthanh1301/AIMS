namespace AIMS.ViewModels.TaskManagement;

public class TaskListVm
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int AssignmentId { get; set; }
    public string InternName { get; set; } = "";
    public string Priority { get; set; } = "MEDIUM";
    public string Status { get; set; } = "TODO";
    public DateTime Deadline { get; set; }
    public decimal? EstimatedHours { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreatedBy { get; set; } = "";
    public bool IsOverdue { get; set; }
}

public class AssignmentListVm
{
    public int Id { get; set; }
    public string InternName { get; set; } = "";
    public string InternEmail { get; set; } = "";
    public string MentorName { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public int TotalTasks { get; set; }
    public int DoneTasks { get; set; }
}