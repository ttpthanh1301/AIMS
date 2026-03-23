using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.Recruitment;

public class ApplicationVm
{
    public int Id { get; set; }
    public int JobDescriptionId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string CVFileUrl { get; set; } = string.Empty;
    public string? CoverLetter { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime ApplyDate { get; set; }
    public decimal? MatchingScore { get; set; }  // Từ AIScreeningResult
    public int? Ranking { get; set; }
}

public class SubmitApplicationRequest
{
    [Required]
    public int JobDescriptionId { get; set; }

    public string? CoverLetter { get; set; }

    // File CV — validate ở Controller
    // IFormFile CVFile
}