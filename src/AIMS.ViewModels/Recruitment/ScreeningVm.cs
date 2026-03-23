namespace AIMS.ViewModels.Recruitment;

public class RankingItemVm
{
    public int Rank { get; set; }
    public int ApplicationId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string CVFileUrl { get; set; } = string.Empty;
    public decimal MatchingScore { get; set; }  // % 0-100
    public string KeywordsMatched { get; set; } = string.Empty;
    public string KeywordsMissing { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ApplyDate { get; set; }
    public DateTime ScreenedAt { get; set; }
}