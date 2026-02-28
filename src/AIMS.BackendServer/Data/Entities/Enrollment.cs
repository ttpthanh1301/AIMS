namespace AIMS.BackendServer.Data.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public DateTime EnrollDate { get; set; } = DateTime.UtcNow;
    public decimal CompletionPercent { get; set; } = 0;
    public DateTime? CompletedDate { get; set; }

    public AppUser InternUser { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
}