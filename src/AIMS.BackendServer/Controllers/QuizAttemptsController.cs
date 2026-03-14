using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using AIMS.BackendServer.Extensions;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class QuizAttemptsController : ControllerBase
{
    private readonly AimsDbContext _context;

    public QuizAttemptsController(AimsDbContext context)
        => _context = context;

    // ─────────────────────────────────────────────────────────
    // POST /api/quizattempts/start
    // Intern bắt đầu làm bài
    // ─────────────────────────────────────────────────────────
    [HttpPost("start")]
    [Authorize(Roles = "Intern")]
    public async Task<IActionResult> Start(
        [FromBody] StartAttemptRequest request)
    {
        var userId = User.GetUserId();

        var quiz = await _context.QuizBanks
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == request.QuizBankId);

        if (quiz == null)
            return NotFound(new { message = "QuizBank không tồn tại." });

        // Kiểm tra số lần thi còn lại
        var attemptCount = await _context.UserQuizAttempts
            .CountAsync(a => a.InternUserId == userId
                          && a.QuizBankId == request.QuizBankId);

        if (attemptCount >= quiz.MaxAttempts)
            return BadRequest(new
            {
                message = $"Bạn đã thi {attemptCount}/{quiz.MaxAttempts} lần. Hết lượt thi."
            });

        var attempt = new UserQuizAttempt
        {
            InternUserId = userId,
            QuizBankId = request.QuizBankId,
            AttemptNumber = attemptCount + 1,
            StartedAt = DateTime.UtcNow,
        };

        _context.UserQuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        // Trả về câu hỏi (ẩn IsCorrect)
        return Ok(new
        {
            AttemptId = attempt.Id,
            AttemptNumber = attempt.AttemptNumber,
            StartedAt = attempt.StartedAt,
            TimeLimit = quiz.TimeLimit,
            TotalQuestions = quiz.Questions.Count,
            Questions = quiz.Questions
                .OrderBy(q => q.SortOrder)
                .Select(q => new
                {
                    q.Id,
                    q.QuestionText,
                    q.QuestionType,
                    q.Score,
                    Options = q.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new
                        {
                            o.Id,
                            o.OptionText,
                            o.SortOrder,
                            // Không trả IsCorrect
                        }),
                }),
        });
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/quizattempts/{attemptId}/submit
    // Intern nộp bài → tự động chấm điểm
    // ─────────────────────────────────────────────────────────
    [HttpPost("{attemptId}/submit")]
    [Authorize(Roles = "Intern")]
    public async Task<IActionResult> Submit(
        int attemptId,
        [FromBody] SubmitAttemptRequest request)
    {
        var userId = User.GetUserId();

        var attempt = await _context.UserQuizAttempts
            .Include(a => a.QuizBank)
                .ThenInclude(q => q.Questions)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId
                                   && a.InternUserId == userId
                                   && a.SubmittedAt == null);

        if (attempt == null)
            return NotFound(new
            {
                message = "Attempt không tồn tại, không thuộc về bạn, hoặc đã nộp rồi."
            });

        // ── Chấm điểm tự động ─────────────────────────────────
        decimal totalScore = 0;
        var answers = new List<UserQuizAnswer>();

        foreach (var answer in request.Answers)
        {
            var question = attempt.QuizBank.Questions
                .FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) continue;

            var selectedOption = question.Options
                .FirstOrDefault(o => o.Id == answer.SelectedOptionId);
            if (selectedOption == null) continue;

            var isCorrect = selectedOption.IsCorrect;
            if (isCorrect)
                totalScore += question.Score;

            answers.Add(new UserQuizAnswer
            {
                AttemptId = attemptId,
                QuestionId = answer.QuestionId,
                SelectedOptionId = answer.SelectedOptionId,
                IsCorrect = isCorrect,
            });
        }

        // ── Tính phần trăm và kết quả ─────────────────────────
        var maxScore = attempt.QuizBank.Questions.Sum(q => q.Score);
        var percent = maxScore > 0 ? (totalScore / maxScore) * 100 : 0;
        var isPassed = percent >= attempt.QuizBank.PassScore;

        attempt.Answers = answers;
        attempt.TotalScore = totalScore;
        attempt.IsPassed = isPassed;
        attempt.SubmittedAt = DateTime.UtcNow;

        _context.UserQuizAnswers.AddRange(answers);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            AttemptId = attemptId,
            TotalScore = totalScore,
            MaxScore = maxScore,
            Percent = Math.Round(percent, 2),
            IsPassed = isPassed,
            PassScore = attempt.QuizBank.PassScore,
            SubmittedAt = attempt.SubmittedAt,
            Message = isPassed
                ? "🎉 Chúc mừng! Bạn đã vượt qua bài kiểm tra!"
                : $"❌ Chưa đạt. Điểm {Math.Round(percent, 1)}% < {attempt.QuizBank.PassScore}%",
        });
    }

    // GET /api/quizattempts?quizBankId=1
    [HttpGet]
    public async Task<IActionResult> GetMyAttempts(
        [FromQuery] int quizBankId)
    {
        var userId = User.GetUserId();

        var attempts = await _context.UserQuizAttempts
            .Where(a => a.InternUserId == userId
                     && a.QuizBankId == quizBankId)
            .OrderByDescending(a => a.StartedAt)
            .Select(a => new
            {
                a.Id,
                a.AttemptNumber,
                a.TotalScore,
                a.IsPassed,
                a.StartedAt,
                a.SubmittedAt,
            })
            .ToListAsync();

        return Ok(attempts);
    }
}

public class StartAttemptRequest
{
    public int QuizBankId { get; set; }
}

public class SubmitAttemptRequest
{
    public List<AnswerItem> Answers { get; set; } = new();
}

public class AnswerItem
{
    public int QuestionId { get; set; }
    public int SelectedOptionId { get; set; }
}