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
    public string InternName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int PendingCount { get; set; }
}

public class QuizAttemptReviewVm
{
    public int AttemptId { get; set; }
    public int QuizBankId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string InternName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int PendingCount { get; set; }
    public List<QuizAttemptPendingAnswerVm> PendingAnswers { get; set; } = new();
}

public class QuizAttemptPendingAnswerVm
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string? AnswerText { get; set; }
}

public class MentorGradeQuizAttemptInputVm
{
    public int AttemptId { get; set; }
    public int QuizBankId { get; set; }
    public List<MentorGradeTextAnswerInputVm> Gradings { get; set; } = new();
}

public class MentorGradeTextAnswerInputVm
{
    public int AnswerId { get; set; }
    public decimal Score { get; set; }
    public string? Feedback { get; set; }
}
