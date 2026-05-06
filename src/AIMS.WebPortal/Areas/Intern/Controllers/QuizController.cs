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

        var quizItems = await Task.WhenAll(quizzes.Select(async quiz =>
        {
            var attempts = await _api.GetAsync<List<QuizAttemptHistoryVm>>(
                $"/api/quizattempts?quizBankId={quiz.Id}") ?? new List<QuizAttemptHistoryVm>();

            var usedAttempts = attempts.Count;
            return new InternQuizListItemVm
            {
                Id = quiz.Id,
                CourseId = quiz.CourseId,
                CourseTitle = quiz.CourseTitle,
                LessonId = quiz.LessonId,
                LessonTitle = quiz.LessonTitle,
                Title = quiz.Title,
                PassScore = quiz.PassScore,
                TimeLimit = quiz.TimeLimit,
                MaxAttempts = quiz.MaxAttempts,
                TotalQuestions = quiz.TotalQuestions,
                CreatedByUser = quiz.CreatedByUser,
                Questions = quiz.Questions,
                UsedAttempts = usedAttempts,
                RemainingAttempts = Math.Max(quiz.MaxAttempts - usedAttempts, 0)
            };
        }));

        return View(quizItems.OrderBy(q => q.CourseTitle).ThenBy(q => q.Title).ToList());
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
        var answersList = new List<Dictionary<string, object?>>();

        foreach (var key in form.Keys.Where(k => k.StartsWith("q_")))
        {
            var questionId = int.Parse(key[2..]);
            var values = form[key];  // StringValues (có thể nhiều giá trị cho MULTIPLE choice)

            // ─── MULTIPLE choice: nhiều giá trị ───
            if (values.Count > 1 || (values.Count == 1 && !string.IsNullOrEmpty(values[0]) && values[0].Contains(',')))
            {
                // MULTIPLE: gửi array các option ID
                var optionIds = values
                    .Where(v => !string.IsNullOrEmpty(v) && int.TryParse(v, out _))
                    .Select(v => int.Parse(v!))
                    .ToList();

                foreach (var optId in optionIds)
                {
                    var answer = new Dictionary<string, object?>();
                    answer["questionId"] = questionId;
                    answer["selectedOptionId"] = optId;
                    answer["answerText"] = null;
                    answersList.Add(answer);
                }
            }
            // ─── SINGLE choice hoặc TEXT: 1 giá trị ───
            else if (values.Count > 0 && !string.IsNullOrEmpty(values[0]))
            {
                var value = values[0];
                var answer = new Dictionary<string, object?>();
                answer["questionId"] = questionId;

                // Kiểm tra xem đáp án là option ID (số) hay text (chuỗi)
                if (int.TryParse(value, out var optionId))
                {
                    answer["selectedOptionId"] = optionId;
                    answer["answerText"] = null;
                }
                else
                {
                    answer["selectedOptionId"] = null;
                    answer["answerText"] = value;
                }

                answersList.Add(answer);
            }
        }

        if (!answersList.Any())
        {
            TempData["Error"] = "Bạn chưa trả lời câu hỏi nào.";
            return RedirectToAction("Index");
        }

        var result = await _api.PostAsync<QuizResultVm>(
            $"/api/quizattempts/{attemptId}/submit",
            new { answers = answersList });

        if (result == null)
        {
            TempData["Error"] = "Lỗi khi nộp bài.";
            return RedirectToAction("Index");
        }
        return View("Result", result);
    }
}

public class QuizAttemptHistoryVm
{
    public int Id { get; set; }
    public int AttemptNumber { get; set; }
    public decimal? TotalScore { get; set; }
    public bool? IsPassed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
