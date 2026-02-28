namespace AIMS.BackendServer.Data.Entities;

public class Timesheet
{
    public int Id { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public int TaskId { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal HoursWorked { get; set; }
    public string? WorkNote { get; set; }

    public AppUser InternUser { get; set; } = null!;
    public TaskItem Task { get; set; } = null!;
}