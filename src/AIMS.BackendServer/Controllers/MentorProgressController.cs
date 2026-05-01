using AIMS.BackendServer.Data;
using AIMS.BackendServer.Extensions;
using AIMS.ViewModels.LMS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Controllers;

/// <summary>
/// API cho Mentor xem tiến độ học tập của Intern
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Mentor,Admin")]
public class MentorProgressController : ControllerBase
{
    private readonly AimsDbContext _context;

    public MentorProgressController(AimsDbContext context)
        => _context = context;

    /// <summary>
    /// GET /api/mentorprogress/mycourses
    /// Lấy danh sách các khóa học của những intern do Mentor quản lý
    /// </summary>
    [HttpGet("mycourses")]
    public async Task<IActionResult> GetMyCourses()
    {
        var mentorId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");

        List<string> internIds = new List<string>();
        if (!isAdmin)
        {
            internIds = await _context.InternAssignments
                .Where(ia => ia.MentorUserId == mentorId)
                .Select(ia => ia.InternUserId)
                .Distinct()
                .ToListAsync();

            if (!internIds.Any())
                return Ok(new List<MentorCourseProgressVm>());
        }

        // Tìm các khóa học của những intern này
        var enrollmentsQuery = _context.Enrollments.AsQueryable();
        if (!isAdmin)
        {
            enrollmentsQuery = enrollmentsQuery.Where(e => internIds.Contains(e.InternUserId));
        }

        var courses = await enrollmentsQuery
            .Include(e => e.Course)
            .Select(e => new
            {
                e.Course.Id,
                e.Course.Title,
                e.Course.Description,
                e.Course.Level,
            })
            .Distinct()
            .ToListAsync();

        var result = new List<MentorCourseProgressVm>();

        foreach (var course in courses)
        {
            var enrollmentsInCourseQuery = _context.Enrollments.Where(e => e.CourseId == course.Id);
            if (!isAdmin)
            {
                enrollmentsInCourseQuery = enrollmentsInCourseQuery.Where(e => internIds.Contains(e.InternUserId));
            }
            var enrollmentsInCourse = await enrollmentsInCourseQuery.ToListAsync();
            var completionPercents = new List<decimal>();

            foreach (var enrollment in enrollmentsInCourse)
            {
                completionPercents.Add(await CalculateCompletionPercentAsync(enrollment.Id, enrollment.CourseId, enrollment.InternUserId));
            }

            result.Add(new MentorCourseProgressVm
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Description = course.Description,
                Level = course.Level,
                TotalInterns = enrollmentsInCourse.Count,
                CompletedInterns = completionPercents.Count(p => p >= 100),
                AverageCompletionPercent = completionPercents.Any()
                    ? Math.Round(completionPercents.Average(), 0, MidpointRounding.AwayFromZero)
                    : 0,
                CreateDate = course.Id > 0 ? DateTime.UtcNow : DateTime.MinValue,
            });
        }

        return Ok(result.OrderByDescending(x => x.CreateDate).ToList());
    }

    /// <summary>
    /// GET /api/mentorprogress/course/{courseId}
    /// Xem chi tiết một khóa học - danh sách intern + tỉ lệ hoàn thành
    /// </summary>
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetCourseDetail(int courseId)
    {
        var mentorId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");

        List<string> internIds = new List<string>();
        if (!isAdmin)
        {
            internIds = await _context.InternAssignments
                .Where(ia => ia.MentorUserId == mentorId)
                .Select(ia => ia.InternUserId)
                .ToListAsync();
        }

        var course = await _context.Courses
            .Include(c => c.Chapters)
                .ThenInclude(ch => ch.Lessons)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
            return NotFound(new { message = "Khóa học không tồn tại." });

        // Nếu không phải Admin và Mentor không có Intern nào, trả về rỗng ngay lập tức
        if (!isAdmin && !internIds.Any())
        {
            var emptyTotalLessons = course.Chapters?.Sum(ch => ch.Lessons?.Count ?? 0) ?? 0;
            var emptyTotalQuizzes = await _context.QuizBanks.CountAsync(q => q.CourseId == courseId);
            return Ok(new MentorCourseDetailVm
            {
                CourseId = courseId,
                CourseTitle = course.Title,
                Description = course.Description,
                Level = course.Level,
                TotalLessons = emptyTotalLessons,
                TotalQuizzes = emptyTotalQuizzes,
                Interns = new List<InternProgressInCourseVm>()
            });
        }

        var enrollmentsQuery = _context.Enrollments.Where(e => e.CourseId == courseId);
        if (!isAdmin)
        {
            enrollmentsQuery = enrollmentsQuery.Where(e => internIds.Contains(e.InternUserId));
        }

        var enrollments = await enrollmentsQuery
            .Include(e => e.InternUser)
            .Include(e => e.LessonProgresses)
            .ToListAsync();

        var totalLessons = course.Chapters?.Sum(ch => ch.Lessons?.Count ?? 0) ?? 0;
        var totalQuizzes = await _context.QuizBanks.CountAsync(q => q.CourseId == courseId);

        var interns = new List<InternProgressInCourseVm>();

        foreach (var e in enrollments)
        {
            var completedLessons = e.LessonProgresses?.Count(lp => lp.IsCompleted) ?? 0;
            var completedQuizzes = await CountPassedQuizzesAsync(courseId, e.InternUserId);
            var percent = CalculateCompletionPercent(totalLessons, totalQuizzes, completedLessons, completedQuizzes);

            interns.Add(new InternProgressInCourseVm
            {
                EnrollmentId = e.Id,
                InternUserId = e.InternUserId,
                InternName = e.InternUser != null ? $"{e.InternUser.FirstName} {e.InternUser.LastName}" : "Unknown",
                InternAvatar = e.InternUser?.Avatar,
                EnrollDate = e.EnrollDate,
                CompletionPercent = percent,
                CompletedLessons = completedLessons,
                TotalLessons = totalLessons,
                CompletedQuizzes = completedQuizzes,
                TotalQuizzes = totalQuizzes,
                CompletedDate = e.CompletedDate,
            });
        }

        return Ok(new MentorCourseDetailVm
        {
            CourseId = courseId,
            CourseTitle = course.Title,
            Description = course.Description,
            Level = course.Level,
            TotalLessons = totalLessons,
            TotalQuizzes = totalQuizzes,
            Interns = interns.OrderByDescending(i => i.CompletionPercent).ToList(),
        });
    }

    /// <summary>
    /// GET /api/mentorprogress/enrollment/{enrollmentId}/details
    /// Xem chi tiết tiến độ bài học của một enrollment
    /// </summary>
    [HttpGet("enrollment/{enrollmentId}/details")]
    public async Task<IActionResult> GetEnrollmentLessonDetails(int enrollmentId)
    {
        var mentorId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");

        var enrollment = await _context.Enrollments
            .Include(e => e.InternUser)
            .Include(e => e.Course)
                .ThenInclude(c => c.Chapters.OrderBy(ch => ch.SortOrder))
                    .ThenInclude(ch => ch.Lessons.OrderBy(l => l.SortOrder))
                        .ThenInclude(l => l.QuizBanks)
            .Include(e => e.LessonProgresses)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment == null)
            return NotFound(new { message = "Enrollment không tồn tại." });

        if (!isAdmin)
        {
            // Kiểm tra mentor có quản lý intern này không
            var isAssignedToMentor = await _context.InternAssignments
                .AnyAsync(ia => ia.MentorUserId == mentorId && ia.InternUserId == enrollment.InternUserId);

            if (!isAssignedToMentor)
                return Forbid();
        }

        var passedAttempts = await _context.UserQuizAttempts
            .Include(a => a.QuizBank)
            .Where(a => a.InternUserId == enrollment.InternUserId
                     && a.QuizBank.CourseId == enrollment.CourseId
                     && a.IsPassed == true)
            .OrderByDescending(a => a.SubmittedAt ?? a.StartedAt)
            .ToListAsync();

        var passedAttemptByQuizId = passedAttempts
            .GroupBy(a => a.QuizBankId)
            .ToDictionary(g => g.Key, g => g.First());

        var chapters = new List<ChapterProgressDetailVm>();
        var completedLessons = enrollment.LessonProgresses?.Count(lp => lp.IsCompleted) ?? 0;
        var totalLessons = enrollment.Course?.Chapters?.Sum(ch => ch.Lessons?.Count ?? 0) ?? 0;
        var totalQuizzes = enrollment.Course?.Chapters?
            .SelectMany(ch => ch.Lessons ?? new List<Data.Entities.Lesson>())
            .Sum(l => l.QuizBanks?.Count ?? 0) ?? 0;
        var completedQuizzes = passedAttemptByQuizId.Count;

        if (enrollment.Course?.Chapters != null)
        {
            foreach (var chapter in enrollment.Course.Chapters)
            {
                var items = new List<CourseProgressItemVm>();

                if (chapter.Lessons == null)
                {
                    chapters.Add(new ChapterProgressDetailVm
                    {
                        ChapterId = chapter.Id,
                        ChapterTitle = chapter.Title,
                        SortOrder = chapter.SortOrder,
                        Items = items,
                    });
                    continue;
                }

                foreach (var lesson in chapter.Lessons)
                {
                    var progress = enrollment.LessonProgresses?
                        .FirstOrDefault(lp => lp.LessonId == lesson.Id);

                    items.Add(new CourseProgressItemVm
                    {
                        ItemId = lesson.Id,
                        ItemTitle = lesson.Title,
                        ItemType = "Lesson",
                        DisplayType = "Bài học",
                        SortOrder = lesson.SortOrder,
                        IsCompleted = progress?.IsCompleted ?? false,
                        CompletedAt = progress?.LastAccessDate,
                    });

                    foreach (var quiz in lesson.QuizBanks.OrderBy(q => q.Id))
                    {
                        passedAttemptByQuizId.TryGetValue(quiz.Id, out var passedAttempt);

                        items.Add(new CourseProgressItemVm
                        {
                            ItemId = quiz.Id,
                            ItemTitle = quiz.Title,
                            ItemType = "Quiz",
                            DisplayType = "Quiz",
                            SortOrder = lesson.SortOrder,
                            IsCompleted = passedAttempt != null,
                            CompletedAt = passedAttempt?.SubmittedAt,
                            Score = passedAttempt?.TotalScore,
                            PassScore = quiz.PassScore,
                        });
                    }
                }

                chapters.Add(new ChapterProgressDetailVm
                {
                    ChapterId = chapter.Id,
                    ChapterTitle = chapter.Title,
                    SortOrder = chapter.SortOrder,
                    Items = items
                        .OrderBy(i => i.SortOrder)
                        .ThenBy(i => i.ItemType == "Lesson" ? 0 : 1)
                        .ThenBy(i => i.ItemId)
                        .ToList(),
                });
            }
        }

        var courseLevelQuizzes = await _context.QuizBanks
            .Where(q => q.CourseId == enrollment.CourseId && q.LessonId == null)
            .OrderBy(q => q.Id)
            .ToListAsync();

        if (courseLevelQuizzes.Any())
        {
            var items = new List<CourseProgressItemVm>();

            foreach (var quiz in courseLevelQuizzes)
            {
                passedAttemptByQuizId.TryGetValue(quiz.Id, out var passedAttempt);

                items.Add(new CourseProgressItemVm
                {
                    ItemId = quiz.Id,
                    ItemTitle = quiz.Title,
                    ItemType = "Quiz",
                    DisplayType = "Quiz",
                    SortOrder = int.MaxValue,
                    IsCompleted = passedAttempt != null,
                    CompletedAt = passedAttempt?.SubmittedAt,
                    Score = passedAttempt?.TotalScore,
                    PassScore = quiz.PassScore,
                });
            }

            chapters.Add(new ChapterProgressDetailVm
            {
                ChapterId = 0,
                ChapterTitle = "Khác",
                SortOrder = int.MaxValue,
                Items = items,
            });
            totalQuizzes += courseLevelQuizzes.Count;
            completedQuizzes = passedAttemptByQuizId.Keys.Count;
        }

        return Ok(new EnrollmentLessonDetailVm
        {
            EnrollmentId = enrollmentId,
            InternUserId = enrollment.InternUserId,
            InternName = enrollment.InternUser != null ? $"{enrollment.InternUser.FirstName} {enrollment.InternUser.LastName}" : "Unknown",
            CourseId = enrollment.CourseId,
            CourseTitle = enrollment.Course?.Title ?? string.Empty,
            CompletionPercent = CalculateCompletionPercent(totalLessons, totalQuizzes, completedLessons, completedQuizzes),
            CompletedLessons = completedLessons,
            TotalLessons = totalLessons,
            CompletedQuizzes = completedQuizzes,
            TotalQuizzes = totalQuizzes,
            Chapters = chapters
                .OrderBy(ch => ch.SortOrder)
                .ToList(),
        });
    }

    private async Task<decimal> CalculateCompletionPercentAsync(int enrollmentId, int courseId, string internUserId)
    {
        var totalLessons = await _context.Lessons
            .CountAsync(l => l.Chapter.CourseId == courseId);

        var totalQuizzes = await _context.QuizBanks
            .CountAsync(q => q.CourseId == courseId);

        var completedLessons = await _context.LessonProgresses
            .CountAsync(lp => lp.EnrollmentId == enrollmentId && lp.IsCompleted);

        var completedQuizzes = await CountPassedQuizzesAsync(courseId, internUserId);

        return CalculateCompletionPercent(totalLessons, totalQuizzes, completedLessons, completedQuizzes);
    }

    private async Task<int> CountPassedQuizzesAsync(int courseId, string internUserId)
    {
        return await _context.UserQuizAttempts
            .Where(a => a.InternUserId == internUserId
                     && a.QuizBank.CourseId == courseId
                     && a.IsPassed == true)
            .Select(a => a.QuizBankId)
            .Distinct()
            .CountAsync();
    }

    private static decimal CalculateCompletionPercent(int totalLessons, int totalQuizzes, int completedLessons, int completedQuizzes)
    {
        var totalItems = totalLessons + totalQuizzes;

        if (totalItems == 0)
        {
            return 100m;
        }

        return Math.Round((decimal)(completedLessons + completedQuizzes) / totalItems * 100m, 0, MidpointRounding.AwayFromZero);
    }
}
