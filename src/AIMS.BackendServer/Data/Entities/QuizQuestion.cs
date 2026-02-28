namespace AIMS.BackendServer.Data.Entities;

public class QuizQuestion
{
    public int Id { get; set; }
    public int QuizBankId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "SINGLE";    // SINGLE / MULTIPLE
    public decimal Score { get; set; } = 1;
    public int SortOrder { get; set; }

    public QuizBank QuizBank { get; set; } = null!;
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}