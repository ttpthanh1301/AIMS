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
    public string QuestionType { get; set; } = "SINGLE";
    public decimal Score { get; set; }
    public List<AttemptOptionVm> Options { get; set; } = new();
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
    public decimal TotalScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percent { get; set; }
    public bool IsPassed { get; set; }
    public decimal PassScore { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}