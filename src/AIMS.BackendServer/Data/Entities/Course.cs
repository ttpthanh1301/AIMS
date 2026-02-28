namespace AIMS.BackendServer.Data.Entities;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Level { get; set; } = "BEGINNER";
    public string CreatedByUserId { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedDate { get; set; }

    public AppUser CreatedByUser { get; set; } = null!;
    public ICollection<CourseChapter> Chapters { get; set; } = new List<CourseChapter>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}