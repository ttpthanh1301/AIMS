using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.TaskManagement;

public class DailyReportVm
{
    public int Id { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public string InternName { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? PlannedTomorrow { get; set; }
    public string? Issues { get; set; }
    public string? MentorFeedback { get; set; }
    public bool HasFeedback { get; set; }
}

public class CreateDailyReportRequest
{
    [Required(ErrorMessage = "Nội dung báo cáo không được để trống")]
    [StringLength(3000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? PlannedTomorrow { get; set; }

    [StringLength(1000)]
    public string? Issues { get; set; }

    public DateTime? ReportDate { get; set; }
    // Null = ngày hôm nay
}

public class MentorFeedbackRequest
{
    [Required(ErrorMessage = "Phản hồi không được để trống")]
    [StringLength(2000)]
    public string Feedback { get; set; } = string.Empty;
}