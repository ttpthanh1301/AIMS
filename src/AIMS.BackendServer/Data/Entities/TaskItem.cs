namespace AIMS.BackendServer.Data.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int AssignmentId { get; set; }
    public string Priority { get; set; } = "MEDIUM";    // LOW / MEDIUM / HIGH
    public string Status { get; set; } = "TODO";
    // TODO / IN_PROGRESS / DONE / OVERDUE
    public DateTime Deadline { get; set; }
    public decimal? EstimatedHours { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;

    public InternAssignment Assignment { get; set; } = null!;
    public AppUser CreatedByUser { get; set; } = null!;
    public ICollection<TaskActivity> Activities { get; set; } = new List<TaskActivity>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}