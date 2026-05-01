using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIMS.ViewModels.LMS;
namespace AIMS.WebPortal.Areas.Intern.Controllers;

[Area("Intern")]
[Authorize(Roles = "Intern")]
public class LMSController : Controller
{
    private readonly BackendApiClient _api;

    public LMSController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Khóa học của tôi";

        // ⭐ Dùng typed ViewModel thay vì dynamic
        var enrollments = await _api.GetAsync<List<EnrollmentVm>>("/api/enrollments")
            ?? new List<EnrollmentVm>();
        var allCourses = await _api.GetAsync<List<CourseVm>>("/api/courses?isPublished=true")
            ?? new List<CourseVm>();

        ViewBag.AllCourses = allCourses;
        return View(enrollments);
    }
    [HttpPost]
    public async Task<IActionResult> Enroll(int courseId)
    {
        await _api.PostAsync<dynamic>("/api/enrollments",
            new { courseId });
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Learn(int courseId)
    {
        ViewData["Title"] = "Học bài";
        var course = await _api.GetAsync<CourseVm>($"/api/courses/{courseId}");
        if (course == null)
        {
            TempData["Error"] = "Khóa học không tồn tại.";
            return RedirectToAction("Index");
        }

        var enrollments = await _api.GetAsync<List<EnrollmentVm>>("/api/enrollments");
        var courseQuizzes = await _api.GetAsync<List<QuizBankVm>>($"/api/quizbanks?courseId={courseId}")
            ?? new List<QuizBankVm>();

        // ⭐ Typed → không còn JsonElement
        var enrollment = enrollments?
            .FirstOrDefault(e => e.CourseId == courseId);

        ViewBag.Enrollment = enrollment;
        ViewBag.CourseQuizzes = courseQuizzes;
        return View(course);
    }

    [HttpPost]
    public async Task<IActionResult> CompleteLesson(
        int enrollmentId, int lessonId, int courseId)
    {
        await _api.PostAsync<dynamic>("/api/lessonprogress/complete",
            new { enrollmentId, lessonId });
        return RedirectToAction("Learn", new { courseId });
    }
}
