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
[Authorize(Roles = "Intern")]
public class LessonProgressController : ControllerBase
{
    private readonly AimsDbContext _context;

    public LessonProgressController(AimsDbContext context)
        => _context = context;

    // ─────────────────────────────────────────────────────────
    // POST /api/lessonprogress/complete
    // Intern đánh dấu 1 lesson đã hoàn thành
    // → Tự động tính lại CompletionPercent của Enrollment
    // ─────────────────────────────────────────────────────────
    [HttpPost("complete")]
    public async Task<IActionResult> MarkComplete(
        [FromBody] MarkCompleteRequest request)
    {
        var userId = User.GetUserId();

        // Tìm enrollment tương ứng
        var enrollment = await _context.Enrollments
            .Include(e => e.LessonProgresses)
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId
                                   && e.InternUserId == userId);

        if (enrollment == null)
            return NotFound(new { message = "Enrollment không tồn tại hoặc không thuộc về bạn." });

        // Kiểm tra lesson thuộc course này không
        var lesson = await _context.Lessons
            .Include(l => l.Chapter)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId
                                   && l.Chapter.CourseId == enrollment.CourseId);
        if (lesson == null)
            return BadRequest(new { message = "Lesson không thuộc course này." });

        // Tìm hoặc tạo LessonProgress
        var progress = enrollment.LessonProgresses
            .FirstOrDefault(p => p.LessonId == request.LessonId);

        if (progress == null)
        {
            progress = new LessonProgress
            {
                EnrollmentId = enrollment.Id,
                LessonId = request.LessonId,
                IsCompleted = true,
                LastAccessDate = DateTime.UtcNow,
            };
            _context.LessonProgresses.Add(progress);
        }
        else
        {
            progress.IsCompleted = true;
            progress.LastAccessDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // ── Tính lại CompletionPercent ────────────────────────
        var completionPercent = await RecalculateCompletionAsync(enrollment);
        enrollment.CompletionPercent = completionPercent;

        // Nếu đạt 100% → set CompletedDate
        if (completionPercent >= 100 && enrollment.CompletedDate == null)
            enrollment.CompletedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            lessonId = request.LessonId,
            enrollmentId = request.EnrollmentId,
            completionPercent = completionPercent,
            isCompleted = completionPercent >= 100,
            message = completionPercent >= 100
                ? "🎉 Chúc mừng! Bạn đã hoàn thành khóa học!"
                : $"Tiến độ: {completionPercent}%",
        });
    }

    // GET /api/lessonprogress/{enrollmentId}
    [HttpGet("{enrollmentId}")]
    public async Task<IActionResult> GetProgress(int enrollmentId)
    {
        var userId = User.GetUserId();

        var progresses = await _context.LessonProgresses
            .Include(p => p.Lesson)
            .Where(p => p.EnrollmentId == enrollmentId
                     && p.Enrollment.InternUserId == userId)
            .Select(p => new
            {
                LessonId = p.LessonId,
                LessonTitle = p.Lesson.Title,
                IsCompleted = p.IsCompleted,
                LastAccessDate = p.LastAccessDate,
            })
            .ToListAsync();

        return Ok(progresses);
    }

    // ── Helper: Tính % hoàn thành ─────────────────────────────
    private async Task<decimal> RecalculateCompletionAsync(
        Enrollment enrollment)
    {
        var totalLessons = await _context.Lessons
            .Include(l => l.Chapter)
            .CountAsync(l => l.Chapter.CourseId == enrollment.CourseId);

        var totalQuizzes = await _context.QuizBanks
            .CountAsync(q => q.CourseId == enrollment.CourseId);

        var totalItems = totalLessons + totalQuizzes;

        if (totalItems == 0) return 100;

        var completedLessons = await _context.LessonProgresses
            .CountAsync(p => p.EnrollmentId == enrollment.Id
                          && p.IsCompleted);

        var completedQuizzes = await _context.UserQuizAttempts
            .Where(a => a.QuizBank.CourseId == enrollment.CourseId
                     && a.InternUserId == enrollment.InternUserId
                     && a.IsPassed == true)
            .Select(a => a.QuizBankId)
            .Distinct()
            .CountAsync();

        return Math.Round((decimal)(completedLessons + completedQuizzes) / totalItems * 100, 0, MidpointRounding.AwayFromZero);
    }
}

public class MarkCompleteRequest
{
    public int EnrollmentId { get; set; }
    public int LessonId { get; set; }
}
