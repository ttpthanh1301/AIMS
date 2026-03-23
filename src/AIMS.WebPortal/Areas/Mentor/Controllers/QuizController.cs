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

    public QuizController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý Quiz";
        var quizzes = await _api.GetAsync<List<QuizBankVm>>("/api/quizbanks")
            ?? new List<QuizBankVm>();
        var courses = await _api.GetAsync<List<CourseVm>>("/api/courses")
            ?? new List<CourseVm>();
        ViewBag.Courses = courses;
        return View(quizzes);
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

        await _api.PostAsync<object>($"/api/quizbanks/{quizId}/questions", new
        {
            quizBankId = quizId,
            questionText,
            questionType,
            score,
            sortOrder = 1,
            options,
        });

        TempData["Success"] = "Thêm câu hỏi thành công!";
        return RedirectToAction("Configure", new { quizId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        int courseId,
        string title,
        decimal passScore,
        int? timeLimit,
        int maxAttempts)
    {
        await _api.PostAsync<object>("/api/quizbanks", new
        {
            courseId,
            title,
            passScore,
            timeLimit,
            maxAttempts,
        });
        TempData["Success"] = "Tạo Quiz thành công!";
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
        string title,
        decimal passScore,
        int? timeLimit,
        int maxAttempts)
    {
        await _api.PutAsync($"/api/quizbanks/{quizId}", new
        {
            courseId,
            title,
            passScore,
            timeLimit,
            maxAttempts,
        });
        TempData["Success"] = "Cập nhật Quiz thành công!";
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