using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.LMS;

public class EnrollmentVm
{
    public int Id { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public string InternName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime EnrollDate { get; set; }
    public decimal CompletionPercent { get; set; }
    public DateTime? CompletedDate { get; set; }
    public bool IsCompleted => CompletionPercent >= 100;
}

public class EnrollCourseRequest
{
    [Required]
    public int CourseId { get; set; }
}