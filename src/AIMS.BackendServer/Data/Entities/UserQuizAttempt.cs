namespace AIMS.BackendServer.Data.Entities;

public class UserQuizAttempt
{
    public int Id { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public int QuizBankId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public decimal? TotalScore { get; set; }
    public bool? IsPassed { get; set; }

    public AppUser InternUser { get; set; } = null!;
    public QuizBank QuizBank { get; set; } = null!;
    public ICollection<UserQuizAnswer> Answers { get; set; } = new List<UserQuizAnswer>();
    public Certificate? Certificate { get; set; }
}