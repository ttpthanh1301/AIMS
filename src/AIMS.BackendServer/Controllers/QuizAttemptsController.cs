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
        int pendingTextAnswers = 0;

        foreach (var answer in request.Answers)
        {
            var question = attempt.QuizBank.Questions
                .FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) continue;

            var userAnswer = new UserQuizAnswer
            {
                AttemptId = attemptId,
                QuestionId = answer.QuestionId,
            };

            // ── Xử lý câu hỏi loại TEXT ─────────────────────────
            if (question.QuestionType == "TEXT")
            {
                userAnswer.AnswerText = answer.AnswerText;
                userAnswer.IsCorrect = null;  // Chưa được chấm
                pendingTextAnswers++;
            }
            // ── Xử lý câu hỏi trắc nghiệm (SINGLE/MULTIPLE) ─────
            else
            {
                var selectedOption = question.Options
                    .FirstOrDefault(o => o.Id == answer.SelectedOptionId);
                if (selectedOption == null) continue;

                userAnswer.SelectedOptionId = answer.SelectedOptionId;
                var isCorrect = selectedOption.IsCorrect;
                userAnswer.IsCorrect = isCorrect;
                
                if (isCorrect)
                    totalScore += question.Score;
            }

            answers.Add(userAnswer);
        }

        // ── Tính phần trăm và kết quả ─────────────────────────
        var maxScore = attempt.QuizBank.Questions.Sum(q => q.Score);
        var percent = maxScore > 0 ? (totalScore / maxScore) * 100 : 0;
        
        // Nếu có câu TEXT chưa chấm, không thể kết luận PASS/FAIL ngay
        bool isPendingReview = pendingTextAnswers > 0;
        var isPassed = isPendingReview ? (bool?)null : (percent >= attempt.QuizBank.PassScore);

        attempt.Answers = answers;
        attempt.TotalScore = isPendingReview ? (decimal?)null : totalScore;
        attempt.IsPassed = isPassed;
        attempt.SubmittedAt = DateTime.UtcNow;

        _context.UserQuizAnswers.AddRange(answers);
        await _context.SaveChangesAsync();

        await UpdateEnrollmentCompletionAsync(attempt.QuizBank.CourseId, userId);

        var message = isPendingReview
            ? $"✏️ Bài thi của bạn có {pendingTextAnswers} câu hỏi tự luận. Mentor sẽ chấm điểm sớm."
            : isPassed == true
                ? "🎉 Chúc mừng! Bạn đã vượt qua bài kiểm tra!"
                : $"❌ Chưa đạt. Điểm {Math.Round(percent, 1)}% < {attempt.QuizBank.PassScore}%";

        return Ok(new
        {
            AttemptId = attemptId,
            TotalScore = attempt.TotalScore,
            MaxScore = maxScore,
            Percent = isPendingReview ? (decimal?)null : Math.Round(percent, 2),
            IsPassed = isPassed,
            PassScore = attempt.QuizBank.PassScore,
            SubmittedAt = attempt.SubmittedAt,
            IsPendingReview = isPendingReview,
            PendingTextAnswers = pendingTextAnswers,
            Message = message,
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

    // ─────────────────────────────────────────────────────────
    // POST /api/quizattempts/{attemptId}/grade-text-answers
    // Mentor chấm điểm câu hỏi tự luận (TEXT)
    // ─────────────────────────────────────────────────────────
    [HttpPost("{attemptId}/grade-text-answers")]
    [Authorize(Roles = "Mentor,Admin")]
    public async Task<IActionResult> GradeTextAnswers(
        int attemptId,
        [FromBody] GradeTextAnswersRequest request)
    {
        var mentorId = User.GetUserId();

        var attempt = await _context.UserQuizAttempts
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.Question)
            .Include(a => a.QuizBank)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
            return NotFound(new { message = "Attempt không tồn tại." });

        // Cập nhật điểm cho các câu trả lời TEXT
        decimal totalScore = 0;
        var hasGradedText = false;

        foreach (var grading in request.Gradings)
        {
            var answer = attempt.Answers
                .FirstOrDefault(a => a.Id == grading.AnswerId);

            if (answer == null || answer.Question.QuestionType != "TEXT")
                continue;

            answer.MentorScore = grading.Score;
            answer.MentorFeedback = grading.Feedback;
            answer.MentorUserId = mentorId;
            answer.ReviewedAt = DateTime.UtcNow;
            answer.IsCorrect = grading.Score > 0;
            hasGradedText = true;
        }

        // Tính lại tổng điểm từ các answer
        foreach (var answer in attempt.Answers)
        {
            if (answer.IsCorrect == true)
            {
                // Nếu là text question, dùng MentorScore, nếu không dùng score tự động
                var score = answer.MentorScore ?? answer.Question.Score;
                totalScore += score;
            }
        }

        if (hasGradedText)
        {
            // Cập nhật tổng điểm và trạng thái PASS/FAIL
            var maxScore = attempt.QuizBank.Questions.Sum(q => q.Score);
            var percent = maxScore > 0 ? (totalScore / maxScore) * 100 : 0;
            var isPassed = percent >= attempt.QuizBank.PassScore;

            attempt.TotalScore = totalScore;
            attempt.IsPassed = isPassed;

            _context.Update(attempt);
            await _context.SaveChangesAsync();
            await UpdateEnrollmentCompletionAsync(attempt.QuizBank.CourseId, attempt.InternUserId);

            return Ok(new
            {
                AttemptId = attemptId,
                TotalScore = totalScore,
                MaxScore = attempt.QuizBank.Questions.Sum(q => q.Score),
                Percent = Math.Round(percent, 2),
                IsPassed = isPassed,
                Message = isPassed
                    ? "✅ Học viên đã vượt qua bài kiểm tra!"
                    : $"⚠️ Học viên chưa đạt. Điểm {Math.Round(percent, 1)}%",
            });
        }

        return BadRequest(new { message = "Không có câu trả lời TEXT nào được cập nhật." });
    }

    private async Task UpdateEnrollmentCompletionAsync(int courseId, string internUserId)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.InternUserId == internUserId);

        if (enrollment == null)
        {
            return;
        }

        var totalLessons = await _context.Lessons
            .CountAsync(l => l.Chapter.CourseId == courseId);

        var totalQuizzes = await _context.QuizBanks
            .CountAsync(q => q.CourseId == courseId);

        var completedLessons = await _context.LessonProgresses
            .CountAsync(lp => lp.EnrollmentId == enrollment.Id && lp.IsCompleted);

        var completedQuizzes = await _context.UserQuizAttempts
            .Where(a => a.InternUserId == internUserId
                     && a.QuizBank.CourseId == courseId
                     && a.IsPassed == true)
            .Select(a => a.QuizBankId)
            .Distinct()
            .CountAsync();

        var totalItems = totalLessons + totalQuizzes;
        var completionPercent = totalItems == 0
            ? 100m
            : Math.Round((decimal)(completedLessons + completedQuizzes) / totalItems * 100m, 0, MidpointRounding.AwayFromZero);

        enrollment.CompletionPercent = completionPercent;

        if (completionPercent >= 100m)
        {
            enrollment.CompletedDate ??= DateTime.UtcNow;
        }
        else
        {
            enrollment.CompletedDate = null;
        }

        await _context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/quizattempts/{attemptId}/pending-reviews
    // Lấy danh sách câu trả lời chờ chấm điểm (cho Mentor)
    // ─────────────────────────────────────────────────────────
    [HttpGet("{attemptId}/pending-reviews")]
    [Authorize(Roles = "Mentor,Admin")]
    public async Task<IActionResult> GetPendingReviews(int attemptId)
    {
        var attempt = await _context.UserQuizAttempts
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.Question)
            .Include(a => a.InternUser)
            .Include(a => a.QuizBank)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
            return NotFound();

        var pendingAnswers = attempt.Answers
            .Where(a => a.Question.QuestionType == "TEXT" && a.MentorScore == null)
            .Select(a => new
            {
                a.Id,
                a.QuestionId,
                a.Question.QuestionText,
                a.Question.Score,
                a.AnswerText,
            })
            .ToList();

        return Ok(new
        {
            AttemptId = attemptId,
            InternName = $"{attempt.InternUser.FirstName} {attempt.InternUser.LastName}",
            QuizTitle = attempt.QuizBank.Title,
            SubmittedAt = attempt.SubmittedAt,
            PendingAnswers = pendingAnswers,
            PendingCount = pendingAnswers.Count,
        });
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/quizattempts/pending-by-quiz?quizBankId=1
    // Danh sách các attempt chờ chấm điểm TEXT cho quiz cụ thể
    // ─────────────────────────────────────────────────────────
    [HttpGet("pending-by-quiz")]
    [Authorize(Roles = "Mentor,Admin")]
    public async Task<IActionResult> GetPendingByQuiz([FromQuery] int quizBankId)
    {
        var attemptsPending = await _context.UserQuizAttempts
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.Question)
            .Include(a => a.InternUser)
            .Include(a => a.QuizBank)
            .Where(a => a.QuizBankId == quizBankId 
                     && a.SubmittedAt != null
                     && a.Answers.Any(ans => ans.Question.QuestionType == "TEXT" && ans.MentorScore == null))
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new
            {
                a.Id,
                a.AttemptNumber,
                InternName = $"{a.InternUser.FirstName} {a.InternUser.LastName}",
                a.SubmittedAt,
                PendingCount = a.Answers.Count(ans => ans.Question.QuestionType == "TEXT" && ans.MentorScore == null),
            })
            .ToListAsync();

        return Ok(attemptsPending);
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
    public int? SelectedOptionId { get; set; }  // Cho câu hỏi trắc nghiệm
    public string? AnswerText { get; set; }  // Cho câu hỏi tự luận (TEXT)
}

public class GradeTextAnswersRequest
{
    public List<TextAnswerGrading> Gradings { get; set; } = new();
}

public class TextAnswerGrading
{
    public int AnswerId { get; set; }
    public decimal Score { get; set; }
    public string? Feedback { get; set; }
}
