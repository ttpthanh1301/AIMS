namespace AIMS.BackendServer.Data.Entities;

public class QuizBank
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal PassScore { get; set; } = 70;
    public int? TimeLimit { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public string CreatedByUserId { get; set; } = string.Empty;

    public Course Course { get; set; } = null!;
    public AppUser CreatedByUser { get; set; } = null!;
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
}