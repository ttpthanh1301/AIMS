namespace AIMS.BackendServer.Data.Entities;

public class Application
{
    public int Id { get; set; }
    public string ApplicantUserId { get; set; } = string.Empty;
    public int JobDescriptionId { get; set; }
    public string CVFileUrl { get; set; } = string.Empty;
    public string? CoverLetter { get; set; }
    public DateTime ApplyDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "PENDING";
    // PENDING / SCREENING / INTERVIEW / ACCEPTED / REJECTED

    public AppUser ApplicantUser { get; set; } = null!;
    public JobDescription JobDescription { get; set; } = null!;
    public CVParsedData? CVParsedData { get; set; }
    public AIScreeningResult? AIScreeningResult { get; set; }
}