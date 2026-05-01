namespace AIMS.BackendServer.Data.Entities;

public class UserQuizAnswer
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? AnswerText { get; set; }
    public bool? IsCorrect { get; set; }
    
    // Fields for TEXT type questions that need mentor grading
    public decimal? MentorScore { get; set; }  // Điểm chấm của mentor
    public string? MentorFeedback { get; set; } // Nhận xét của mentor
    public string? MentorUserId { get; set; } // ID của mentor chấm
    public DateTime? ReviewedAt { get; set; } // Thời gian chấm

    public UserQuizAttempt Attempt { get; set; } = null!;
    public QuizQuestion Question { get; set; } = null!;
    public QuestionOption? SelectedOption { get; set; }
}
