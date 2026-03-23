namespace AIMS.ViewModels.LMS;

public class QuizBankListVm
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal PassScore { get; set; }
    public int? TimeLimit { get; set; }
    public int MaxAttempts { get; set; }
    public int TotalQuestions { get; set; }
    public string CreatedByUser { get; set; } = "";
}

public class QuizBankDetailVm
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal PassScore { get; set; }
    public int? TimeLimit { get; set; }
    public int MaxAttempts { get; set; }
    public int TotalQuestions { get; set; }
    public List<QuizQuestionDetailVm> Questions { get; set; } = new();
}

public class QuizQuestionDetailVm
{
    public int Id { get; set; }
    public int QuizBankId { get; set; }
    public string QuestionText { get; set; } = "";
    public string QuestionType { get; set; } = "SINGLE";
    public decimal Score { get; set; }
    public int SortOrder { get; set; }
    public List<QuestionOptionDetailVm> Options { get; set; } = new();
}

public class QuestionOptionDetailVm
{
    public int Id { get; set; }
    public string OptionText { get; set; } = "";
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}

// ── Thêm các Request classes còn thiếu ────────────────────────

public class CreateQuizBankRequest
{
    public int CourseId { get; set; }
    public string Title { get; set; } = "";
    public decimal PassScore { get; set; } = 70;
    public int? TimeLimit { get; set; }
    public int MaxAttempts { get; set; } = 3;
}

public class CreateQuestionRequest
{
    public int QuizBankId { get; set; }
    public string QuestionText { get; set; } = "";
    public string QuestionType { get; set; } = "SINGLE";
    public decimal Score { get; set; } = 1;
    public int SortOrder { get; set; } = 1;
    public List<CreateOptionRequest> Options { get; set; } = new();
}

public class CreateOptionRequest
{
    public string OptionText { get; set; } = "";
    public bool IsCorrect { get; set; } = false;
    public int SortOrder { get; set; } = 1;
}
public class QuizBankVm : QuizBankListVm
{
    public List<QuizQuestionVm> Questions { get; set; } = new();
}

public class QuizQuestionVm
{
    public int Id { get; set; }
    public int QuizBankId { get; set; }
    public string QuestionText { get; set; } = "";
    public string QuestionType { get; set; } = "SINGLE";
    public decimal Score { get; set; }
    public int SortOrder { get; set; }
    public List<QuestionOptionVm> Options { get; set; } = new();
}

public class QuestionOptionVm
{
    public int Id { get; set; }
    public string OptionText { get; set; } = "";
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}