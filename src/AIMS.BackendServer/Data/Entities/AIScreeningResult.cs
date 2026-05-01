namespace AIMS.BackendServer.Data.Entities;

public class AIScreeningResult
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }      // Quan hệ 1-1
    public decimal MatchingScore { get; set; }  // % Cosine Similarity
    public int? Ranking { get; set; }
    public string? KeywordsMatched { get; set; }
    public string? KeywordsMissing { get; set; }
    public string ProcessingStatus { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public DateTime ScreenedAt { get; set; } = DateTime.UtcNow;
    public string? ReviewedByHRId { get; set; }

    public Application Application { get; set; } = null!;
    public AppUser? ReviewedByHR { get; set; }
}
