using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.LMS;

public class LessonVm
{
    public int Id { get; set; }
    public int ChapterId { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public int? DurationMinutes { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
}

public class CreateLessonRequest
{
    [Required]
    public int ChapterId { get; set; }

    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string LessonType { get; set; } = "VIDEO";

    [StringLength(500)]
    public string? ContentUrl { get; set; }

    public int? DurationMinutes { get; set; }
    public int SortOrder { get; set; } = 1;
    public bool IsRequired { get; set; } = true;
}

public class UpdateLessonRequest
{
    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string LessonType { get; set; } = "VIDEO";

    [StringLength(500)]
    public string? ContentUrl { get; set; }

    public int? DurationMinutes { get; set; }
    public int SortOrder { get; set; } = 1;
    public bool IsRequired { get; set; } = true;
}