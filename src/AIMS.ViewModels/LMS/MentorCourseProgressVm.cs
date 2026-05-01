namespace AIMS.ViewModels.LMS;

/// <summary>
/// Mentor xem danh sách các khóa học của những intern do anh ta quản lý
/// </summary>
public class MentorCourseProgressVm
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Level { get; set; } = string.Empty;
    public int TotalInterns { get; set; }
    public int CompletedInterns { get; set; }
    public decimal AverageCompletionPercent { get; set; }
    public DateTime CreateDate { get; set; }
}

/// <summary>
/// Chi tiết một khóa học - Danh sách intern + tỉ lệ hoàn thành
/// </summary>
public class MentorCourseDetailVm
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Level { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public int TotalQuizzes { get; set; }
    public List<InternProgressInCourseVm> Interns { get; set; } = new();
}

/// <summary>
/// Tiến độ của một intern trong khóa học
/// </summary>
public class InternProgressInCourseVm
{
    public int EnrollmentId { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public string InternName { get; set; } = string.Empty;
    public string? InternAvatar { get; set; }
    public DateTime EnrollDate { get; set; }
    public decimal CompletionPercent { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedQuizzes { get; set; }
    public int TotalQuizzes { get; set; }
    public DateTime? CompletedDate { get; set; }
    public bool IsCompleted => CompletionPercent >= 100;

    public string Status => CompletionPercent switch
    {
        0m => "Chưa bắt đầu",
        >= 100m => "Hoàn thành",
        _ => "Đang học"
    };
}

/// <summary>
/// Chi tiết từng bài học trong enrollment
/// </summary>
public class EnrollmentLessonDetailVm
{
    public int EnrollmentId { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public string InternName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal CompletionPercent { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedQuizzes { get; set; }
    public int TotalQuizzes { get; set; }
    public string Status => CompletionPercent switch
    {
        0m => "Chưa bắt đầu",
        >= 100m => "Hoàn thành",
        _ => "Đang học"
    };
    public List<ChapterProgressDetailVm> Chapters { get; set; } = new();
}

public class ChapterProgressDetailVm
{
    public int ChapterId { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<CourseProgressItemVm> Items { get; set; } = new();
}

public class CourseProgressItemVm
{
    public int ItemId { get; set; }
    public string ItemTitle { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string DisplayType { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? Score { get; set; }
    public decimal? PassScore { get; set; }
    public int SortOrder { get; set; }
}
