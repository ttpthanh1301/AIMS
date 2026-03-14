using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.LMS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIMS.BackendServer.Extensions;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LessonsController : ControllerBase
{
    private readonly AimsDbContext _context;

    public LessonsController(AimsDbContext context)
        => _context = context;

    // GET /api/lessons/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Chapter)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
            return NotFound(new { message = $"Lesson #{id} không tồn tại." });

        return Ok(new LessonVm
        {
            Id = lesson.Id,
            ChapterId = lesson.ChapterId,
            ChapterTitle = lesson.Chapter.Title,
            Title = lesson.Title,
            LessonType = lesson.LessonType,
            ContentUrl = lesson.ContentUrl,
            DurationMinutes = lesson.DurationMinutes,
            SortOrder = lesson.SortOrder,
            IsRequired = lesson.IsRequired,
        });
    }

    // GET /api/lessons?chapterId=1
    [HttpGet]
    public async Task<IActionResult> GetByChapter([FromQuery] int chapterId)
    {
        var lessons = await _context.Lessons
            .Include(l => l.Chapter)
            .Where(l => l.ChapterId == chapterId)
            .OrderBy(l => l.SortOrder)
            .Select(l => new LessonVm
            {
                Id = l.Id,
                ChapterId = l.ChapterId,
                ChapterTitle = l.Chapter.Title,
                Title = l.Title,
                LessonType = l.LessonType,
                ContentUrl = l.ContentUrl,
                DurationMinutes = l.DurationMinutes,
                SortOrder = l.SortOrder,
                IsRequired = l.IsRequired,
            })
            .ToListAsync();

        return Ok(lessons);
    }

    // POST /api/lessons
    [HttpPost]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Create(
        [FromBody] CreateLessonRequest request)
    {
        var validTypes = new[] { "VIDEO", "DOCUMENT", "QUIZ" };
        if (!validTypes.Contains(request.LessonType.ToUpper()))
            return BadRequest(new { message = "LessonType phải là VIDEO, DOCUMENT hoặc QUIZ." });

        var chapterExists = await _context.CourseChapters
            .AnyAsync(ch => ch.Id == request.ChapterId);
        if (!chapterExists)
            return NotFound(new { message = $"Chapter #{request.ChapterId} không tồn tại." });

        var lesson = new Lesson
        {
            ChapterId = request.ChapterId,
            Title = request.Title,
            LessonType = request.LessonType.ToUpper(),
            ContentUrl = request.ContentUrl,
            DurationMinutes = request.DurationMinutes,
            SortOrder = request.SortOrder,
            IsRequired = request.IsRequired,
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = lesson.Id },
            new { lesson.Id, lesson.Title, lesson.LessonType });
    }

    // PUT /api/lessons/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateLessonRequest request)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson == null)
            return NotFound(new { message = $"Lesson #{id} không tồn tại." });

        lesson.Title = request.Title;
        lesson.LessonType = request.LessonType.ToUpper();
        lesson.ContentUrl = request.ContentUrl;
        lesson.DurationMinutes = request.DurationMinutes;
        lesson.SortOrder = request.SortOrder;
        lesson.IsRequired = request.IsRequired;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật lesson thành công." });
    }

    // DELETE /api/lessons/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Delete(int id)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson == null)
            return NotFound(new { message = $"Lesson #{id} không tồn tại." });

        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa lesson." });
    }
}