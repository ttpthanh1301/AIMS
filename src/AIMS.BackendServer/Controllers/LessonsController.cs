using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.LMS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIMS.BackendServer.Extensions;
using System.Text.Json;
using System.Text.RegularExpressions;

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

        return Ok(ToLessonVm(lesson));
    }

    // GET /api/lessons?chapterId=1
    [HttpGet]
    public async Task<IActionResult> GetByChapter([FromQuery] int chapterId)
    {
        var lessons = await _context.Lessons
            .Include(l => l.Chapter)
            .Where(l => l.ChapterId == chapterId)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();

        return Ok(lessons.Select(ToLessonVm).ToList());
    }

    // POST /api/lessons
    [HttpPost]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Create(
        [FromBody] CreateLessonRequest request)
    {
        var validTypes = new[] { "TEXT", "VIDEO", "SLIDE", "DOCUMENT", "QUIZ", "MIXED" };
        if (!validTypes.Contains(request.LessonType.ToUpper()))
            return BadRequest(new { message = "LessonType phải là TEXT, VIDEO, SLIDE, DOCUMENT, QUIZ hoặc MIXED." });

        var chapterExists = await _context.CourseChapters
            .AnyAsync(ch => ch.Id == request.ChapterId);
        if (!chapterExists)
            return NotFound(new { message = $"Chapter #{request.ChapterId} không tồn tại." });

        var content = BuildContent(request.Description, request.VideoUrl, request.SlideUrl, request.ImageUrls, request.ContentUrl);
        if (!content.Success)
            return BadRequest(new { message = content.Message });

        var lesson = new Lesson
        {
            ChapterId = request.ChapterId,
            Title = request.Title,
            LessonType = request.LessonType.ToUpper(),
            ContentUrl = content.Json,
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
        var content = BuildContent(request.Description, request.VideoUrl, request.SlideUrl, request.ImageUrls, request.ContentUrl);
        if (!content.Success)
            return BadRequest(new { message = content.Message });

        lesson.ContentUrl = content.Json;
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

    private static LessonVm ToLessonVm(Lesson lesson)
    {
        var content = ParseContent(lesson.ContentUrl);
        return new LessonVm
        {
            Id = lesson.Id,
            ChapterId = lesson.ChapterId,
            ChapterTitle = lesson.Chapter.Title,
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

    private static (bool Success, string? Message, string? Json) BuildContent(
        string? description,
        string? videoUrl,
        string? slideUrl,
        List<string>? imageUrls,
        string? legacyContentUrl)
    {
        var payload = new LessonContentPayload
        {
            Description = description?.Trim(),
            VideoUrl = videoUrl?.Trim(),
            SlideUrl = slideUrl?.Trim(),
            ImageUrls = imageUrls?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? new()
        };

        if (string.IsNullOrWhiteSpace(payload.Description)
            && string.IsNullOrWhiteSpace(payload.VideoUrl)
            && string.IsNullOrWhiteSpace(payload.SlideUrl)
            && payload.ImageUrls.Count == 0
            && string.IsNullOrWhiteSpace(legacyContentUrl))
        {
            return (false, "Bài học phải có ít nhất một nội dung: text, video, slide hoặc ảnh.", null);
        }

        if (!string.IsNullOrWhiteSpace(payload.VideoUrl))
        {
            payload.VideoEmbedUrl = TryBuildYouTubeEmbedUrl(payload.VideoUrl);
            if (payload.VideoEmbedUrl == null)
                return (false, "Link YouTube không hợp lệ.", null);
        }

        if (!string.IsNullOrWhiteSpace(payload.SlideUrl))
        {
            payload.SlideEmbedUrl = TryBuildGoogleEmbedUrl(payload.SlideUrl);
            if (payload.SlideEmbedUrl == null)
                return (false, "Link Google Docs/Slides không hợp lệ.", null);
        }

        if (string.IsNullOrWhiteSpace(payload.Description)
            && string.IsNullOrWhiteSpace(payload.VideoUrl)
            && string.IsNullOrWhiteSpace(payload.SlideUrl)
            && payload.ImageUrls.Count == 0)
        {
            payload.Description = legacyContentUrl;
        }

        return (true, null, JsonSerializer.Serialize(payload));
    }

    private static LessonContentPayload ParseContent(string? contentUrl)
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

    private static string? TryBuildYouTubeEmbedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var host = uri.Host.ToLowerInvariant();
        string? videoId = null;

        if (host.Contains("youtu.be"))
        {
            videoId = uri.AbsolutePath.Trim('/');
        }
        else if (host.Contains("youtube.com"))
        {
            if (uri.AbsolutePath.StartsWith("/embed/"))
                videoId = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            else
                videoId = uri.Query
                    .TrimStart('?')
                    .Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Split('=', 2))
                    .FirstOrDefault(parts => parts.Length == 2 && parts[0] == "v")?[1];
        }

        return Regex.IsMatch(videoId ?? "", "^[A-Za-z0-9_-]{6,}$")
            ? $"https://www.youtube.com/embed/{videoId}"
            : null;
    }

    private static string? TryBuildGoogleEmbedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var host = uri.Host.ToLowerInvariant();
        if (!host.EndsWith("google.com") && !host.EndsWith("docs.google.com") && !host.EndsWith("slides.google.com"))
            return null;

        var path = uri.AbsolutePath;
        var match = Regex.Match(path, @"/(?:presentation|document)/d/([^/]+)");
        if (!match.Success)
            return null;

        var type = path.Contains("/document/") ? "document" : "presentation";
        var id = match.Groups[1].Value;
        return $"https://docs.google.com/{type}/d/{id}/embed";
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
