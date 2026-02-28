namespace AIMS.BackendServer.Data.Entities;

public class QuestionOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; } = false;
    public int SortOrder { get; set; }

    public QuizQuestion Question { get; set; } = null!;
}