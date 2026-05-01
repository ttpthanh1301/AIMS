using AIMS.ViewModels.LMS;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Mentor.Controllers;

[Area("Mentor")]
[Authorize(Roles = "Mentor,Admin")]
public class QuizController : Controller
{
    private readonly BackendApiClient _api;
    private readonly ILogger<QuizController> _logger;

    public QuizController(BackendApiClient api, ILogger<QuizController> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý Quiz";

        var (quizzes, quizError) = await _api.GetWithMessageAsync<List<QuizBankVm>>("/api/quizbanks");
        var (courses, courseError) = await _api.GetWithMessageAsync<List<CourseVm>>("/api/courses");

        if (!string.IsNullOrWhiteSpace(quizError))
        {
            _logger.LogWarning("Unable to load quizzes: {Message}", quizError);
            ViewBag.Error = quizError;
        }

        if (!string.IsNullOrWhiteSpace(courseError))
        {
            _logger.LogWarning("Unable to load courses for quiz page: {Message}", courseError);
            ViewBag.Error ??= courseError;
        }

        ViewBag.Courses = courses;
        return View(quizzes ?? new List<QuizBankVm>());
    }

    [HttpGet]
    public async Task<IActionResult> LessonsByCourse(int courseId)
    {
        var course = await _api.GetAsync<CourseVm>($"/api/courses/{courseId}");
        if (course == null)
            return Json(Array.Empty<object>());

        var chapters = (course.Chapters ?? new List<ChapterVm>())
            .OrderBy(ch => ch.SortOrder)
            .Select(ch => new
            {
                id = ch.Id,
                title = ch.Title,
                label = $"{ch.SortOrder}. {ch.Title}",
                lessons = ch.Lessons
                    .OrderBy(l => l.SortOrder)
                    .Select(l => new
                    {
                        id = l.Id,
                        title = l.Title,
                        label = $"{l.SortOrder}. {l.Title}"
                    })
                    .ToList()
            })
            .ToList();

        return Json(chapters);
    }

    public async Task<IActionResult> Configure(int quizId)
    {
        ViewData["Title"] = "Cấu hình Quiz";
        var quiz = await _api.GetAsync<QuizBankVm>($"/api/quizbanks/{quizId}");
        return View(quiz);
    }

    [HttpPost]
    public async Task<IActionResult> AddQuestion(
        int quizId,
        string questionText,
        string questionType,
        decimal score,
        string[] optionTexts,
        string[] isCorrects)
    {
        var options = new List<object>();
        for (int i = 0; i < optionTexts.Length; i++)
        {
            var text = optionTexts[i];
            if (string.IsNullOrWhiteSpace(text)) continue;

            bool correct = i < isCorrects.Length
                && isCorrects[i] == "true";

            options.Add(new
            {
                optionText = text,
                isCorrect = correct,
                sortOrder = i + 1,
            });
        }

        if (!options.Any())
        {
            TempData["Error"] = "Vui lòng nhập ít nhất 1 đáp án.";
            return RedirectToAction("Configure", new { quizId });
        }

        var (success, message) = await _api.PostWithMessageAsync($"/api/quizbanks/{quizId}/questions", new
        {
            quizBankId = quizId,
            questionText,
            questionType,
            score,
            sortOrder = 1,
            options,
        });

        if (success)
            TempData["Success"] = message ?? "Thêm câu hỏi thành công!";
        else
            TempData["Error"] = message ?? "Không thể thêm câu hỏi.";

        return RedirectToAction("Configure", new { quizId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        int courseId,
        int? lessonId,
        string title,
        decimal passScore,
        int? timeLimit,
        int maxAttempts)
    {
        var (success, message) = await _api.PostWithMessageAsync("/api/quizbanks", new
        {
            courseId,
            lessonId,
            title,
            passScore,
            timeLimit,
            maxAttempts,
        });

        if (success)
            TempData["Success"] = message ?? "Tạo Quiz thành công!";
        else
            TempData["Error"] = message ?? "Tạo Quiz thất bại.";

        return RedirectToAction("Index");
    }
    // Thêm 2 action này vào QuizController

    [HttpGet]
    public async Task<IActionResult> Edit(int quizId)
    {
        ViewData["Title"] = "Chỉnh sửa Quiz";
        var quiz = await _api.GetAsync<QuizBankVm>($"/api/quizbanks/{quizId}");
        var courses = await _api.GetAsync<List<CourseVm>>("/api/courses")
            ?? new List<CourseVm>();
        ViewBag.Courses = courses;
        return View(quiz);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        int quizId,
        int courseId,
        int? lessonId,
        string title,
        decimal passScore,
        int? timeLimit,
        int maxAttempts)
    {
        var (success, message) = await _api.PutWithMessageAsync($"/api/quizbanks/{quizId}", new
        {
            courseId,
            lessonId,
            title,
            passScore,
            timeLimit,
            maxAttempts,
        });

        if (success)
            TempData["Success"] = message ?? "Cập nhật Quiz thành công!";
        else
            TempData["Error"] = message ?? "Không thể cập nhật Quiz.";

        return RedirectToAction("Index");
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int quizId)
    {
        var (success, message) = await _api.DeleteWithMessageAsync(
            $"/api/quizbanks/{quizId}");

        if (success)
            TempData["Success"] = "Đã xóa Quiz thành công!";
        else
            TempData["Error"] = message ?? "Không thể xóa Quiz này.";

        return RedirectToAction("Index");
    }
}
