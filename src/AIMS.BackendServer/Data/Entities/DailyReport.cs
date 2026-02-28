namespace AIMS.BackendServer.Data.Entities;

public class DailyReport
{
    public int Id { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? PlannedTomorrow { get; set; }
    public string? Issues { get; set; }
    public string? MentorFeedback { get; set; }
    public string? ReviewedByMentorId { get; set; }

    public AppUser InternUser { get; set; } = null!;
    public AppUser? ReviewedByMentor { get; set; }
}