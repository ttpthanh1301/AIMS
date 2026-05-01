namespace AIMS.ViewModels.LMS;

public class QuizAttemptVm
{
    public int AttemptId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime StartedAt { get; set; }
    public int? TimeLimit { get; set; }
    public int TotalQuestions { get; set; }
    public List<AttemptQuestionVm> Questions { get; set; } = new();
}

public class AttemptQuestionVm
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "SINGLE";  // SINGLE, MULTIPLE, TEXT
    public decimal Score { get; set; }
    public List<AttemptOptionVm> Options { get; set; } = new();  // Rỗng nếu QuestionType = TEXT
}

public class AttemptOptionVm
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class QuizResultVm
{
    public int AttemptId { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal? Percent { get; set; }
    public bool? IsPassed { get; set; }
    public decimal PassScore { get; set; }
    public DateTime SubmittedAt { get; set; }
    public bool IsPendingReview { get; set; }  // Có câu text chưa được chấm?
    public int PendingTextAnswers { get; set; }  // Số câu text chưa chấm
    public string Message { get; set; } = string.Empty;
}

public class QuizAttemptReviewListVm
{
    public int AttemptId { get; set; }
    public int QuizBankId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string InternName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int PendingTextAnswers { get; set; }
}

public class QuizAttemptReviewVm
{
    public int AttemptId { get; set; }
    public int QuizBankId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string InternName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public decimal PassScore { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal? Percent { get; set; }
    public bool? IsPassed { get; set; }
    public int PendingTextAnswers { get; set; }
    public List<QuizAttemptReviewQuestionVm> Questions { get; set; } = new();
}

public class QuizAttemptReviewQuestionVm
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "SINGLE";  // SINGLE, MULTIPLE, TEXT
    public decimal Score { get; set; }
    public string? AnswerText { get; set; }  // Đáp án của intern (cho TEXT type)
    public int? SelectedOptionId { get; set; }
    public string? SelectedOptionText { get; set; }
    public bool? IsCorrect { get; set; }
    
    // Fields for TEXT question review by mentor
    public decimal? MentorScore { get; set; }  // Điểm chấm bởi mentor
    public string? MentorFeedback { get; set; }  // Nhận xét của mentor
    public bool IsReviewed { get; set; }  // Đã được mentor chấm?
    public DateTime? ReviewedAt { get; set; }  // Thời gian chấm
    
    public List<QuizAttemptReviewOptionVm> Options { get; set; } = new();
}

public class QuizAttemptReviewOptionVm
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}
