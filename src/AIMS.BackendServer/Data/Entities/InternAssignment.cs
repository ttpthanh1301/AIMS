namespace AIMS.BackendServer.Data.Entities;

public class InternAssignment
{
    public int Id { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public string MentorUserId { get; set; } = string.Empty;
    public int PeriodId { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    public AppUser InternUser { get; set; } = null!;
    public AppUser MentorUser { get; set; } = null!;
    public InternshipPeriod Period { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}