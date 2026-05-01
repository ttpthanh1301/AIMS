using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIMS.ViewModels.LMS;
using AIMS.WebPortal.Services;

namespace AIMS.WebPortal.Areas.Mentor.Controllers;

/// <summary>
/// Controller cho Mentor xem tiến độ học tập của Intern
/// </summary>
[Area("Mentor")]
[Authorize(Roles = "Mentor,Admin")]
public class ProgressController : Controller
{
    private readonly BackendApiClient _api;
    private readonly ILogger<ProgressController> _logger;

    public ProgressController(BackendApiClient api, ILogger<ProgressController> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>
    /// GET /Mentor/Progress/Dashboard
    /// Mentor dashboard hiển thị tổng quan tiến độ của tất cả intern
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            var courses = await _api.GetAsync<List<MentorCourseProgressVm>>("/api/mentorprogress/mycourses");
            
            if (courses == null)
            {
                ViewBag.Message = "Không thể tải dữ liệu dashboard. Vui lòng đăng nhập lại.";
                ViewBag.TotalCourses = 0;
                ViewBag.TotalInterns = 0;
                ViewBag.CompletedInterns = 0;
                ViewBag.AverageCompletion = 0m;
                return View(new List<MentorCourseProgressVm>());
            }

            // Tính toán thống kê
            ViewBag.TotalCourses = courses.Count;
            ViewBag.TotalInterns = courses.Sum(c => c.TotalInterns);
            ViewBag.CompletedInterns = courses.Sum(c => c.CompletedInterns);
            ViewBag.AverageCompletion = courses.Any() ? courses.Average(c => c.AverageCompletionPercent) : 0m;

            return View(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading mentor dashboard");
            ViewBag.Message = "Có lỗi xảy ra khi tải dashboard.";
            ViewBag.TotalCourses = 0;
            ViewBag.TotalInterns = 0;
            ViewBag.CompletedInterns = 0;
            ViewBag.AverageCompletion = 0m;
            return View(new List<MentorCourseProgressVm>());
        }
    }

    /// <summary>
    /// GET /Mentor/Progress/Courses
    /// Hiển thị danh sách khóa học của những intern do mentor quản lý
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Courses()
    {
        try
        {
            var courses = await _api.GetAsync<List<MentorCourseProgressVm>>("/api/mentorprogress/mycourses");
            
            if (courses == null)
            {
                ViewBag.Message = "Không thể tải danh sách khóa học.";
                return View(new List<MentorCourseProgressVm>());
            }

            return View(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching mentor courses");
            ViewBag.Message = "Có lỗi xảy ra khi tải danh sách khóa học.";
            return View(new List<MentorCourseProgressVm>());
        }
    }

    /// <summary>
    /// GET /Mentor/Progress/CourseDetail/{id}
    /// Hiển thị chi tiết khóa học - danh sách intern và tiến độ của họ
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CourseDetail(int id)
    {
        try
        {
            var courseDetail = await _api.GetAsync<MentorCourseDetailVm>($"/api/mentorprogress/course/{id}");
            
            if (courseDetail == null)
            {
                ViewBag.Message = "Không thể tải chi tiết khóa học.";
                return View(null);
            }

            return View(courseDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course detail for {CourseId}", id);
            ViewBag.Message = "Có lỗi xảy ra khi tải chi tiết khóa học.";
            return View(null);
        }
    }

    /// <summary>
    /// GET /Mentor/Progress/LessonProgress/{id}
    /// Hiển thị chi tiết từng bài học của một enrollment
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LessonProgress(int id)
    {
        try
        {
            var lessonDetails = await _api.GetAsync<EnrollmentLessonDetailVm>($"/api/mentorprogress/enrollment/{id}/details");
            
            if (lessonDetails == null)
            {
                ViewBag.Message = "Không thể tải chi tiết bài học.";
                return View(null);
            }

            return View(lessonDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lesson progress for {EnrollmentId}", id);
            ViewBag.Message = "Có lỗi xảy ra khi tải chi tiết bài học.";
            return View(null);
        }
    }
}
