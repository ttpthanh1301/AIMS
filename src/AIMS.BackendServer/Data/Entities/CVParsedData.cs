namespace AIMS.BackendServer.Data.Entities;

public class CVParsedData
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }      // Quan hệ 1-1
    public string? FullName { get; set; }
    public string? EmailExtracted { get; set; }
    public string? PhoneExtracted { get; set; }
    public string? SkillsExtracted { get; set; }
    public string? EducationExtracted { get; set; }
    public string? ExperienceExtracted { get; set; }
    public string? RawText { get; set; }
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;

    public Application Application { get; set; } = null!;
}