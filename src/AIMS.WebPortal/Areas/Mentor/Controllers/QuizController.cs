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

    [HttpGet]
    public async Task<IActionResult> PendingReviews(int quizId)
    {
        ViewData["Title"] = "Bài chờ chấm";

        var quiz = await _api.GetAsync<QuizBankVm>($"/api/quizbanks/{quizId}");
        if (quiz == null)
        {
            TempData["Error"] = "Không tìm thấy quiz.";
            return RedirectToAction(nameof(Index));
        }

        var pendingAttempts = await _api.GetAsync<List<QuizAttemptReviewListVm>>(
            $"/api/quizattempts/pending-by-quiz?quizBankId={quizId}") ?? new List<QuizAttemptReviewListVm>();

        ViewBag.Quiz = quiz;
        return View(pendingAttempts);
    }

    [HttpGet]
    public async Task<IActionResult> ReviewAttempt(int attemptId)
    {
        ViewData["Title"] = "Chấm bài tự luận";

        var review = await _api.GetAsync<QuizAttemptReviewVm>(
            $"/api/quizattempts/{attemptId}/pending-reviews");

        if (review == null)
        {
            TempData["Error"] = "Không tải được bài làm cần chấm hoặc bài đã được chấm xong.";
            return RedirectToAction(nameof(Index));
        }

        return View(review);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewAttempt(MentorGradeQuizAttemptInputVm input)
    {
        if (input.AttemptId <= 0)
        {
            TempData["Error"] = "Dữ liệu chấm bài không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        input.Gradings = input.Gradings
            .Where(g => g.AnswerId > 0)
            .ToList();

        if (!input.Gradings.Any())
        {
            TempData["Error"] = "Không có câu trả lời nào để chấm.";
            return RedirectToAction(nameof(ReviewAttempt), new { attemptId = input.AttemptId });
        }

        var (success, message) = await _api.PostWithMessageAsync(
            $"/api/quizattempts/{input.AttemptId}/grade-text-answers",
            new
            {
                gradings = input.Gradings.Select(g => new
                {
                    answerId = g.AnswerId,
                    score = g.Score,
                    feedback = g.Feedback,
                })
            });

        if (success)
        {
            TempData["Success"] = message ?? "Đã chấm bài thành công.";
            return RedirectToAction(nameof(PendingReviews), new { quizId = input.QuizBankId });
        }

        TempData["Error"] = message ?? "Không thể lưu kết quả chấm bài.";
        return RedirectToAction(nameof(ReviewAttempt), new { attemptId = input.AttemptId });
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
        // Normalize questionType to lowercase for comparison
        var normalizedType = (questionType ?? "").ToLower();

        var options = new List<object>();

        // Chỉ xử lý options cho loại câu hỏi không phải text
        if (normalizedType != "text")
        {
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

            // Chỉ bắt buộc có đáp án nếu không phải loại text
            if (!options.Any())
            {
                TempData["Error"] = "Vui lòng nhập ít nhất 1 đáp án.";
                return RedirectToAction("Configure", new { quizId });
            }
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
