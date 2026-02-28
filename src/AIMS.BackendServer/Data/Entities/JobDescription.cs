namespace AIMS.BackendServer.Data.Entities;

public class JobDescription
{
    public int Id { get; set; }
    public int JobPositionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DetailContent { get; set; } = string.Empty;
    public string RequiredSkills { get; set; } = string.Empty;  // Input cho AI
    public decimal? MinGPA { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string Status { get; set; } = "OPEN";                // OPEN / CLOSED
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeadlineDate { get; set; }

    public JobPosition JobPosition { get; set; } = null!;
    public AppUser CreatedByUser { get; set; } = null!;
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}