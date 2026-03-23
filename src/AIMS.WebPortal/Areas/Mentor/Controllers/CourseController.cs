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

    public CourseController(BackendApiClient api)
        => _api = api;

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
        string level, bool isPublished)
    {
        await _api.PostAsync<object>("/api/courses", new
        {
            title,
            description,
            level,
            isPublished
        });
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
        string level,
        bool isPublished)
    {
        await _api.PutAsync($"/api/courses/{courseId}", new
        {
            title,
            description,
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
        string lessonType,
        string? contentUrl,
        int? durationMinutes,
        int sortOrder)
    {
        await _api.PostAsync<object>("/api/lessons", new
        {
            chapterId,
            title,
            lessonType,
            contentUrl,
            durationMinutes,
            sortOrder,
            isRequired = true,
        });
        TempData["Success"] = "Thêm bài học thành công!";
        return RedirectToAction("ManageLessons", new { courseId });
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
}