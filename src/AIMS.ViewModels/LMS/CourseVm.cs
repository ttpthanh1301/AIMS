using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.LMS;

// ── Course Response ────────────────────────────────────────────
public class CourseVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Level { get; set; } = "BEGINNER";
    public bool IsPublished { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreatedByUser { get; set; } = string.Empty;
    public int TotalChapters { get; set; }
    public int TotalLessons { get; set; }
    public int TotalEnrollments { get; set; }
    public List<ChapterVm> Chapters { get; set; } = new();
}

public class ChapterVm
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<LessonSummaryVm> Lessons { get; set; } = new();
}

public class LessonSummaryVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public int? DurationMinutes { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
}

// ── Create/Update Course ───────────────────────────────────────
public class CreateCourseRequest
{
    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    public string Level { get; set; } = "BEGINNER";
}

public class UpdateCourseRequest
{
    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    public string Level { get; set; } = "BEGINNER";
    public bool IsPublished { get; set; }
}

// ── Create/Update Chapter ──────────────────────────────────────
public class CreateChapterRequest
{
    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 1;
}

public class UpdateChapterRequest
{
    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 1;
}