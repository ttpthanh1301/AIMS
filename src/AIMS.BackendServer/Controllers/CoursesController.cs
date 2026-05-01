using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.LMS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using AIMS.BackendServer.Extensions;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly AimsDbContext _context;

    public CoursesController(AimsDbContext context)
        => _context = context;

    // ─────────────────────────────────────────────────────────
    // GET /api/courses?isPublished=true&level=BEGINNER
    // ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isPublished = null,
        [FromQuery] string? level = null)
    {
        var query = _context.Courses
            .Include(c => c.CreatedByUser)
            .Include(c => c.Chapters)
                .ThenInclude(ch => ch.Lessons)
            .Include(c => c.Enrollments)
            .AsQueryable();

        if (isPublished.HasValue)
            query = query.Where(c => c.IsPublished == isPublished.Value);

        if (!string.IsNullOrEmpty(level))
            query = query.Where(c => c.Level == level.ToUpper());

        // Intern chỉ thấy course đã published
        if (User.IsInRole("Intern"))
            query = query.Where(c => c.IsPublished);

        var result = await query
            .OrderByDescending(c => c.CreateDate)
            .Select(c => new CourseVm
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                Level = c.Level,
                IsPublished = c.IsPublished,
                CreateDate = c.CreateDate,
                CreatedByUser = c.CreatedByUser.FirstName + " " +
                                   c.CreatedByUser.LastName,
                TotalChapters = c.Chapters.Count,
                TotalLessons = c.Chapters.SelectMany(ch => ch.Lessons).Count(),
                TotalEnrollments = c.Enrollments.Count,
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET /api/courses/{id}/enrollments
    // Mentor/Admin xem danh sách intern đã đăng ký và tỉ lệ hoàn thành
    [HttpGet("{id}/enrollments")]
    [Authorize(Roles = "Mentor,Admin")]
    public async Task<IActionResult> GetEnrollmentsForCourse(int id)
    {
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == id);
        if (!courseExists)
            return NotFound(new { message = $"Course #{id} không tồn tại." });

        var enrollments = await _context.Enrollments
            .Include(e => e.InternUser)
            .Where(e => e.CourseId == id)
            .OrderByDescending(e => e.EnrollDate)
            .Select(e => new
            {
                e.Id,
                e.InternUserId,
                InternName = e.InternUser.FirstName + " " + e.InternUser.LastName,
                e.CompletionPercent,
                e.EnrollDate,
                e.CompletedDate,
            })
            .ToListAsync();

        return Ok(new { CourseId = id, Enrollments = enrollments });
    }
    // ─────────────────────────────────────────────────────────
    // GET /api/courses/{id}  — Trả về cả cây Chapters + Lessons
    // ─────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await _context.Courses
            .Include(c => c.CreatedByUser)
            .Include(c => c.Chapters.OrderBy(ch => ch.SortOrder))
                .ThenInclude(ch => ch.Lessons.OrderBy(l => l.SortOrder))
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return NotFound(new { message = $"Course #{id} không tồn tại." });

        // Intern chỉ xem được course đã published
        if (User.IsInRole("Intern") && !course.IsPublished)
            return Forbid();

        return Ok(new CourseVm
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            Level = course.Level,
            IsPublished = course.IsPublished,
            CreateDate = course.CreateDate,
            CreatedByUser = $"{course.CreatedByUser.FirstName} {course.CreatedByUser.LastName}",
            TotalChapters = course.Chapters.Count,
            TotalLessons = course.Chapters.SelectMany(ch => ch.Lessons).Count(),
            TotalEnrollments = course.Enrollments.Count,
            Chapters = course.Chapters.Select(ch => new ChapterVm
            {
                Id = ch.Id,
                CourseId = ch.CourseId,
                Title = ch.Title,
                SortOrder = ch.SortOrder,
                Lessons = ch.Lessons.Select(ToLessonSummaryVm).ToList(),
            }).ToList(),
        });
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/courses  (Mentor/Admin tạo khóa học)
    // ─────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCourseRequest request)
    {
        var userId = User.GetUserId();

        // Debug — xóa sau khi fix xong
        if (string.IsNullOrEmpty(userId))
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value });
            return BadRequest(new { error = "userId empty", claims });
        }

        var course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            Level = request.Level.ToUpper(),
            IsPublished = request.IsPublished,
            CreateDate = DateTime.UtcNow,
            CreatedByUserId = userId,
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = course.Id },
            new { course.Id, course.Title, course.IsPublished });
    }
    // ─────────────────────────────────────────────────────────
    // PUT /api/courses/{id}
    // ─────────────────────────────────────────────────────────
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateCourseRequest request)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
            return NotFound(new { message = $"Course #{id} không tồn tại." });

        course.Title = request.Title;
        course.Description = request.Description;
        course.ThumbnailUrl = request.ThumbnailUrl;
        course.Level = request.Level.ToUpper();
        course.IsPublished = request.IsPublished;
        course.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật course thành công." });
    }

    // ─────────────────────────────────────────────────────────
    // DELETE /api/courses/{id}
    // ─────────────────────────────────────────────────────────
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return NotFound(new { message = $"Course #{id} không tồn tại." });

        if (course.Enrollments.Any())
            return BadRequest(new
            {
                message = $"Course đang có {course.Enrollments.Count} intern đăng ký. Không thể xóa."
            });

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa course." });
    }

    // ═════════════════════════════════════════════════════════
    // CHAPTERS — Nested resource /api/courses/{courseId}/chapters
    // ═════════════════════════════════════════════════════════

    // GET /api/courses/{courseId}/chapters
    [HttpGet("{courseId}/chapters")]
    public async Task<IActionResult> GetChapters(int courseId)
    {
        var chapters = await _context.CourseChapters
            .Include(ch => ch.Lessons)
            .Where(ch => ch.CourseId == courseId)
            .OrderBy(ch => ch.SortOrder)
            .ToListAsync();

        var result = chapters
            .Select(ch => new ChapterVm
            {
                Id = ch.Id,
                CourseId = ch.CourseId,
                Title = ch.Title,
                SortOrder = ch.SortOrder,
                Lessons = ch.Lessons
                    .OrderBy(l => l.SortOrder)
                    .Select(ToLessonSummaryVm)
                    .ToList(),
            })
            .ToList();

        return Ok(result);
    }

    // POST /api/courses/{courseId}/chapters
    [HttpPost("{courseId}/chapters")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> CreateChapter(
        int courseId, [FromBody] CreateChapterRequest request)
    {
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == courseId);
        if (!courseExists)
            return NotFound(new { message = $"Course #{courseId} không tồn tại." });

        var chapter = new CourseChapter
        {
            CourseId = courseId,
            Title = request.Title,
            SortOrder = request.SortOrder,
        };

        _context.CourseChapters.Add(chapter);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetChapters),
            new { courseId },
            new { chapter.Id, chapter.Title, chapter.SortOrder });
    }

    // PUT /api/courses/{courseId}/chapters/{chapterId}
    [HttpPut("{courseId}/chapters/{chapterId}")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> UpdateChapter(
        int courseId, int chapterId,
        [FromBody] UpdateChapterRequest request)
    {
        var chapter = await _context.CourseChapters
            .FirstOrDefaultAsync(ch => ch.Id == chapterId
                                    && ch.CourseId == courseId);

        if (chapter == null)
            return NotFound(new { message = "Chapter không tồn tại." });

        chapter.Title = request.Title;
        chapter.SortOrder = request.SortOrder;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật chapter thành công." });
    }

    // DELETE /api/courses/{courseId}/chapters/{chapterId}
    [HttpDelete("{courseId}/chapters/{chapterId}")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> DeleteChapter(int courseId, int chapterId)
    {
        var chapter = await _context.CourseChapters
            .Include(ch => ch.Lessons)
            .FirstOrDefaultAsync(ch => ch.Id == chapterId
                                    && ch.CourseId == courseId);

        if (chapter == null)
            return NotFound(new { message = "Chapter không tồn tại." });

        if (chapter.Lessons.Any())
            return BadRequest(new
            {
                message = $"Chapter còn {chapter.Lessons.Count} lesson. Xóa lesson trước."
            });

        _context.CourseChapters.Remove(chapter);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa chapter." });
    }

    private static LessonSummaryVm ToLessonSummaryVm(Lesson lesson)
    {
        var content = ParseLessonContent(lesson.ContentUrl);
        return new LessonSummaryVm
        {
            Id = lesson.Id,
            Title = lesson.Title,
            LessonType = lesson.LessonType,
            ContentUrl = lesson.ContentUrl,
            Description = content.Description,
            VideoUrl = content.VideoUrl,
            VideoEmbedUrl = content.VideoEmbedUrl,
            SlideUrl = content.SlideUrl,
            SlideEmbedUrl = content.SlideEmbedUrl,
            ImageUrls = content.ImageUrls,
            DurationMinutes = lesson.DurationMinutes,
            SortOrder = lesson.SortOrder,
            IsRequired = lesson.IsRequired,
        };
    }

    private static LessonContentPayload ParseLessonContent(string? contentUrl)
    {
        if (string.IsNullOrWhiteSpace(contentUrl))
            return new LessonContentPayload();

        try
        {
            var payload = JsonSerializer.Deserialize<LessonContentPayload>(contentUrl, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new LessonContentPayload();
            payload.ImageUrls ??= new();
            return payload;
        }
        catch
        {
            return new LessonContentPayload { Description = contentUrl };
        }
    }

    private class LessonContentPayload
    {
        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
        public string? VideoEmbedUrl { get; set; }
        public string? SlideUrl { get; set; }
        public string? SlideEmbedUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }
}
