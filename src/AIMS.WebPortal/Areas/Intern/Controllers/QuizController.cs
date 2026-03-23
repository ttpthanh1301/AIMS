using AIMS.ViewModels.LMS;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Intern.Controllers;

[Area("Intern")]
[Authorize(Roles = "Intern")]
public class QuizController : Controller
{
    private readonly BackendApiClient _api;
    public QuizController(BackendApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Bài kiểm tra";
        var quizzes = await _api.GetAsync<List<QuizBankVm>>("/api/quizbanks")
            ?? new List<QuizBankVm>();
        return View(quizzes);
    }

    [HttpPost]
    public async Task<IActionResult> Start(int quizBankId)
    {
        var attempt = await _api.GetAsync<QuizAttemptVm>(
            $"/api/quizattempts/start-get/{quizBankId}");

        // Dùng PostAsync vì API là POST
        var result = await _api.PostAsync<QuizAttemptVm>(
            "/api/quizattempts/start", new { quizBankId });

        if (result == null)
        {
            TempData["Error"] = "Không thể bắt đầu bài thi. Có thể đã hết lượt.";
            return RedirectToAction("Index");
        }
        return View("TakeQuiz", result);
    }

    [HttpPost]
    public async Task<IActionResult> Submit(
        int attemptId, IFormCollection form)
    {
        var answers = form.Keys
            .Where(k => k.StartsWith("q_"))
            .Select(k => new
            {
                questionId = int.Parse(k[2..]),
                selectedOptionId = int.Parse(form[k]!),
            })
            .Cast<object>().ToList();

        var result = await _api.PostAsync<QuizResultVm>(
            $"/api/quizattempts/{attemptId}/submit",
            new { answers });

        if (result == null)
        {
            TempData["Error"] = "Lỗi khi nộp bài.";
            return RedirectToAction("Index");
        }
        return View("Result", result);
    }
}