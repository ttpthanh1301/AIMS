namespace AIMS.BackendServer.Data.Entities;

public class Lesson
{
    public int Id { get; set; }
    public int ChapterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string LessonType { get; set; } = "VIDEO";   // VIDEO / DOCUMENT / QUIZ
    public string? ContentUrl { get; set; }
    public int? DurationMinutes { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = true;

    public Data.Entities.CourseChapter Chapter { get; set; } = null!;
    public ICollection<LessonProgress> Progresses { get; set; } = new List<LessonProgress>();
}