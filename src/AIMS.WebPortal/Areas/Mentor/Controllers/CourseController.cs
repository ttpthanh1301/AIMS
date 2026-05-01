using AIMS.ViewModels.LMS;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Mentor.Controllers;

[Area("Mentor")]
[Authorize(Roles = "Mentor,Admin")]
public class CourseController : Controller
{
    private readonly BackendApiClient _api;
    private readonly IWebHostEnvironment _environment;

    public CourseController(BackendApiClient api, IWebHostEnvironment environment)
    {
        _api = api;
        _environment = environment;
    }

    // ── GET /Mentor/Course ────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý Khóa học";
        var courses = await _api.GetAsync<List<CourseVm>>("/api/courses")
            ?? new List<CourseVm>();
        return View(courses);
    }

    // ── GET /Mentor/Course/Create ─────────────────────────────
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Tạo Khóa học";
        return View();
    }

    // ── POST /Mentor/Course/Create ────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create(
        string title, string? description,
        string? thumbnailUrl, string level, bool isPublished)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Tên khóa học không được để trống.";
            return View();
        }

        var created = await _api.PostAsync<object>("/api/courses", new
        {
            title,
            description,
            thumbnailUrl,
            level,
            isPublished
        });
        if (created == null)
        {
            TempData["Error"] = "Tạo khóa học thất bại.";
            return View();
        }

        TempData["Success"] = "Tạo khóa học thành công!";
        return RedirectToAction("Index");
    }

    // ── GET /Mentor/Course/Edit/{courseId} ────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(int courseId)
    {
        ViewData["Title"] = "Chỉnh sửa Khóa học";
        var course = await _api.GetAsync<CourseVm>($"/api/courses/{courseId}");
        if (course == null)
        {
            TempData["Error"] = "Khóa học không tồn tại.";
            return RedirectToAction("Index");
        }
        return View(course);
    }

    // ── POST /Mentor/Course/Edit ──────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Edit(
        int courseId,
        string title,
        string? description,
        string? thumbnailUrl,
        string level,
        bool isPublished)
    {
        await _api.PutAsync($"/api/courses/{courseId}", new
        {
            title,
            description,
            thumbnailUrl,
            level,
            isPublished
        });
        TempData["Success"] = "Cập nhật khóa học thành công!";
        return RedirectToAction("Index");
    }

    // ── POST /Mentor/Course/Delete ────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Delete(int courseId)
    {
        var (success, message) = await _api.DeleteWithMessageAsync(
            $"/api/courses/{courseId}");

        if (success)
            TempData["Success"] = "Đã xóa khóa học thành công!";
        else
            TempData["Error"] = message ?? "Không thể xóa khóa học này.";

        return RedirectToAction("Index");
    }

    // ── POST /Mentor/Course/Publish ───────────────────────────
    [HttpPost]
    public async Task<IActionResult> Publish(int courseId, string isPublished)
    {
        bool publish = isPublished.ToLower() == "true";

        var course = await _api.GetAsync<CourseVm>($"/api/courses/{courseId}");
        if (course != null)
        {
            await _api.PutAsync($"/api/courses/{courseId}", new
            {
                title = course.Title,
                description = course.Description,
                thumbnailUrl = course.ThumbnailUrl,
                level = course.Level,
                isPublished = publish,
            });
        }
        return RedirectToAction("Index");
    }

    // ── GET /Mentor/Course/ManageLessons ──────────────────────
    public async Task<IActionResult> ManageLessons(int courseId)
    {
        ViewData["Title"] = "Quản lý Bài học";
        var course = await _api.GetAsync<CourseVm>($"/api/courses/{courseId}");
        if (course == null)
        {
            TempData["Error"] = "Khóa học không tồn tại.";
            return RedirectToAction("Index");
        }

        ViewBag.CourseId = courseId;
        return View(course);
    }

    // ── POST /Mentor/Course/AddChapter ────────────────────────
    [HttpPost]
    public async Task<IActionResult> AddChapter(
        int courseId, string title, int sortOrder)
    {
        await _api.PostAsync<object>(
            $"/api/courses/{courseId}/chapters",
            new { title, sortOrder });
        TempData["Success"] = "Thêm chương thành công!";
        return RedirectToAction("ManageLessons", new { courseId });
    }

    // ── POST /Mentor/Course/UpdateChapter ─────────────────────
    [HttpPost]
    public async Task<IActionResult> UpdateChapter(
        int courseId, int chapterId, string title, int sortOrder)
    {
        var (success, message) = await _api.PutWithMessageAsync(
            $"/api/courses/{courseId}/chapters/{chapterId}",
            new { title, sortOrder });

        if (!success)
            TempData["Error"] = message ?? "Không thể cập nhật chương này.";
        else
            TempData["Success"] = "Đã cập nhật chương.";

        return RedirectToAction("ManageLessons", new { courseId });
    }

    // ── POST /Mentor/Course/DeleteChapter ─────────────────────
    [HttpPost]
    public async Task<IActionResult> DeleteChapter(int courseId, int chapterId)
    {
        var (success, message) = await _api.DeleteWithMessageAsync(
            $"/api/courses/{courseId}/chapters/{chapterId}");

        if (!success)
            TempData["Error"] = message ?? "Không thể xóa chương này.";
        else
            TempData["Success"] = "Đã xóa chương.";

        return RedirectToAction("ManageLessons", new { courseId });
    }

    // ── POST /Mentor/Course/AddLesson ─────────────────────────
    [HttpPost]
    public async Task<IActionResult> AddLesson(
        int courseId,
        int chapterId,
        string title,
        string? description,
        string? videoUrl,
        string? slideUrl,
        string? imageUrls,
        int? durationMinutes,
        int sortOrder)
    {
        var images = ParseImageUrls(imageUrls);
        if (string.IsNullOrWhiteSpace(description)
            && string.IsNullOrWhiteSpace(videoUrl)
            && string.IsNullOrWhiteSpace(slideUrl)
            && images.Count == 0)
        {
            TempData["Error"] = "Bài học phải có ít nhất một nội dung: text, video, slide hoặc ảnh.";
            return RedirectToAction("ManageLessons", new { courseId });
        }

        var created = await _api.PostAsync<object>("/api/lessons", new
        {
            chapterId,
            title,
            lessonType = "MIXED",
            description,
            videoUrl,
            slideUrl,
            imageUrls = images,
            durationMinutes,
            sortOrder,
            isRequired = true,
        });
        if (created == null)
        {
            TempData["Error"] = "Thêm bài học thất bại. Kiểm tra link YouTube/Google Docs/Slides.";
            return RedirectToAction("ManageLessons", new { courseId });
        }

        TempData["Success"] = "Thêm bài học thành công!";
        return RedirectToAction("ManageLessons", new { courseId });
    }

    // ── GET /Mentor/Course/LessonDetails ─────────────────────
    [HttpGet]
    public async Task<IActionResult> LessonDetails(int courseId, int lessonId)
    {
        var lesson = await _api.GetAsync<LessonVm>($"/api/lessons/{lessonId}");
        if (lesson == null)
        {
            TempData["Error"] = "Bài học không tồn tại.";
            return RedirectToAction("ManageLessons", new { courseId });
        }

        ViewBag.CourseId = courseId;
        return View(lesson);
    }

    // ── GET /Mentor/Course/EditLesson ────────────────────────
    [HttpGet]
    public async Task<IActionResult> EditLesson(int courseId, int lessonId)
    {
        var lesson = await _api.GetAsync<LessonVm>($"/api/lessons/{lessonId}");
        if (lesson == null)
        {
            TempData["Error"] = "Bài học không tồn tại.";
            return RedirectToAction("ManageLessons", new { courseId });
        }

        ViewBag.CourseId = courseId;
        return View(lesson);
    }

    // ── POST /Mentor/Course/EditLesson ───────────────────────
    [HttpPost]
    public async Task<IActionResult> EditLesson(
        int courseId,
        int lessonId,
        string title,
        string? description,
        string? videoUrl,
        string? slideUrl,
        string? imageUrls,
        int? durationMinutes,
        int sortOrder,
        bool isRequired = true)
    {
        var images = ParseImageUrls(imageUrls);
        if (string.IsNullOrWhiteSpace(description)
            && string.IsNullOrWhiteSpace(videoUrl)
            && string.IsNullOrWhiteSpace(slideUrl)
            && images.Count == 0)
        {
            TempData["Error"] = "Bài học phải có ít nhất một nội dung: text, video, slide hoặc ảnh.";
            return RedirectToAction("EditLesson", new { courseId, lessonId });
        }

        var (success, message) = await _api.PutWithMessageAsync(
            $"/api/lessons/{lessonId}",
            new
            {
                title,
                lessonType = "MIXED",
                description,
                videoUrl,
                slideUrl,
                imageUrls = images,
                durationMinutes,
                sortOrder,
                isRequired,
            });

        if (!success)
        {
            TempData["Error"] = message ?? "Cập nhật bài học thất bại.";
            return RedirectToAction("EditLesson", new { courseId, lessonId });
        }

        TempData["Success"] = "Cập nhật bài học thành công!";
        return RedirectToAction("ManageLessons", new { courseId });
    }

    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile image)
    {
        if (image == null || image.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn ảnh." });

        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedContentTypes.Contains(image.ContentType))
            return BadRequest(new { message = "Chỉ hỗ trợ JPG, PNG, GIF hoặc WebP." });

        const long maxFileSize = 5 * 1024 * 1024;
        if (image.Length > maxFileSize)
            return BadRequest(new { message = "Ảnh không được vượt quá 5MB." });

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "Định dạng ảnh không hợp lệ." });

        var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads", "courses");
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadRoot, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await image.CopyToAsync(stream);
        }

        return Ok(new { url = Url.Content($"~/uploads/courses/{fileName}") });
    }

    // ── POST /Mentor/Course/DeleteLesson ──────────────────────
    [HttpPost]
    public async Task<IActionResult> DeleteLesson(int courseId, int lessonId)
    {
        var (success, message) = await _api.DeleteWithMessageAsync(
            $"/api/lessons/{lessonId}");

        if (!success)
            TempData["Error"] = message ?? "Không thể xóa bài học này.";
        else
            TempData["Success"] = "Đã xóa bài học.";

        return RedirectToAction("ManageLessons", new { courseId });
    }

    private static List<string> ParseImageUrls(string? imageUrls)
    {
        return (imageUrls ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
}
