using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.Recruitment;

public class JobDescriptionVm
{
    public int Id { get; set; }
    public int JobPositionId { get; set; }
    public string JobPositionTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DetailContent { get; set; } = string.Empty;
    public string RequiredSkills { get; set; } = string.Empty;
    public decimal? MinGPA { get; set; }
    public string Status { get; set; } = "OPEN";
    public DateTime CreateDate { get; set; }
    public DateTime? DeadlineDate { get; set; }
    public string CreatedByUser { get; set; } = string.Empty;
    public int TotalApplications { get; set; }
}

public class CreateJobDescriptionRequest
{
    [Required]
    public int JobPositionId { get; set; }

    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string DetailContent { get; set; } = string.Empty;

    [Required(ErrorMessage = "RequiredSkills dùng để AI so khớp CV — bắt buộc nhập")]
    public string RequiredSkills { get; set; } = string.Empty;
    // VD: "C# .NET React SQL Docker Azure JWT REST API"

    public decimal? MinGPA { get; set; }
    public DateTime? DeadlineDate { get; set; }
}

public class UpdateJobDescriptionRequest
{
    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string DetailContent { get; set; } = string.Empty;

    [Required]
    public string RequiredSkills { get; set; } = string.Empty;

    public decimal? MinGPA { get; set; }
    public DateTime? DeadlineDate { get; set; }

    [Required]
    public string Status { get; set; } = "OPEN"; // OPEN / CLOSED
}