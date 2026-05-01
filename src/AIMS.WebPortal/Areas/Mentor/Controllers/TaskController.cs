using AIMS.ViewModels.TaskManagement;
using AIMS.WebPortal.Models.Admin;
using AIMS.WebPortal.Models;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Mentor.Controllers;

[Area("Mentor")]
[Authorize(Roles = "Mentor,Admin")]
public class TaskController : Controller
{
    private readonly BackendApiClient _api;
    private readonly IWebHostEnvironment _environment;

    public TaskController(BackendApiClient api, IWebHostEnvironment environment)
    {
        _api = api;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Kanban Board";

        var tasks = await _api.GetAsync<List<TaskVm>>(
            "/api/tasks") ?? new();

        return View(tasks);
    }

    [HttpGet]
    public async Task<IActionResult> CreateTask()
    {
        ViewData["Title"] = "Tạo task mới";
        ViewBag.Assignments = await GetAssignmentsAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask(
        string title, string? description, int assignmentId,
        string priority, string deadline,
        decimal? estimatedHours)
    {
        if (string.IsNullOrWhiteSpace(title) || assignmentId <= 0 || string.IsNullOrWhiteSpace(deadline))
        {
            TempData["Error"] = "Vui lòng nhập tiêu đề, người phụ trách và deadline.";
            ViewBag.Assignments = await GetAssignmentsAsync();
            return View();
        }

        if (!DateTime.TryParse(deadline, out var parsedDeadline))
        {
            TempData["Error"] = "Deadline không hợp lệ.";
            ViewBag.Assignments = await GetAssignmentsAsync();
            return View();
        }

        var (success, message) = await _api.PostWithMessageAsync("/api/tasks", new
        {
            title,
            description,
            assignmentId,
            priority,
            deadline = parsedDeadline,
            estimatedHours,
        });

        if (!success)
        {
            TempData["Error"] = message ?? "Tạo task thất bại. Vui lòng thử lại.";
            ViewBag.Assignments = await GetAssignmentsAsync();
            return View();
        }

        TempData["Success"] = "Tạo task thành công.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        string title, string? description, int assignmentId,
        string priority, string deadline,
        decimal? estimatedHours)
    {
        return await CreateTask(title, description, assignmentId, priority, deadline, estimatedHours);
    }

    [HttpGet]
    public async Task<IActionResult> EditTask(int taskId)
    {
        if (taskId <= 0)
            return RedirectToAction(nameof(Index));

        var task = await _api.GetAsync<TaskDetailVm>($"/api/tasks/{taskId}");
        if (task == null)
        {
            TempData["Error"] = "Không tìm thấy task hoặc bạn không có quyền chỉnh sửa.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Chỉnh sửa task";
        ViewBag.Assignments = await GetAssignmentsAsync();
        return View(task);
    }

    [HttpPost]
    public async Task<IActionResult> EditTask(
        int id, string title, string? description,
        string priority, string deadline,
        decimal? estimatedHours)
    {
        if (id <= 0)
            return RedirectToAction(nameof(Index));

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(deadline))
        {
            TempData["Error"] = "Vui lòng nhập tiêu đề và deadline.";
            return await ReloadEditViewAsync(id);
        }

        if (!DateTime.TryParse(deadline, out var parsedDeadline))
        {
            TempData["Error"] = "Deadline không hợp lệ.";
            return await ReloadEditViewAsync(id);
        }

        var updated = await _api.PutAsync($"/api/tasks/{id}", new
        {
            title,
            description,
            priority,
            deadline = parsedDeadline,
            estimatedHours,
        });

        if (!updated)
        {
            TempData["Error"] = "Cập nhật task thất bại. Vui lòng thử lại.";
            return await ReloadEditViewAsync(id);
        }

        TempData["Success"] = "Cập nhật task thành công.";
        return RedirectToAction(nameof(TaskDetail), new { taskId = id });
    }

    [HttpPost]
    public async Task<IActionResult> UploadTaskImage(IFormFile image)
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

        var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads", "tasks");
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadRoot, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await image.CopyToAsync(stream);
        }

        var imageUrl = Url.Content($"~/uploads/tasks/{fileName}");
        return Ok(new { url = imageUrl });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        if (taskId <= 0)
        {
            TempData["Error"] = "Task không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var deleted = await _api.DeleteWithMessageAsync($"/api/tasks/{taskId}");
        if (!deleted.Success)
        {
            TempData["Error"] = deleted.Message ?? "Xóa task thất bại.";
            return RedirectToAction(nameof(TaskDetail), new { taskId });
        }

        TempData["Success"] = deleted.Message ?? "Đã xóa task.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Mentor,Admin")]
    public async Task<IActionResult> UpdateStatus([FromBody] KanbanStatusUpdateRequest request)
    {
        if (request.Id <= 0 || string.IsNullOrEmpty(request.Status))
            return BadRequest(new { message = "Dữ liệu không hợp lệ" });

        try
        {
            var response = await _api.PutAsync(
                $"/api/tasks/{request.Id}/status",
                new
                {
                    status = request.Status.ToUpper(),
                    note = request.Note ?? "Cập nhật từ Kanban board"
                });

            if (!response)
                return BadRequest(new { message = "Cập nhật thất bại" });

            return Ok(new
            {
                success = true,
                message = $"Cập nhật task sang '{request.Status}' thành công",
                newStatus = request.Status.ToUpper()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> MentorDetail(string mentorId)
    {
        var mentor = await _api.GetAsync<MentorDetailVm>($"/api/mentors/{mentorId}");
        if (mentor == null)
        {
            return NotFound();
        }
        return View(mentor);
    }

    [HttpGet]
    public async Task<IActionResult> TaskDetail(int taskId)
    {
        if (taskId <= 0)
            return RedirectToAction(nameof(Index));

        var task = await _api.GetAsync<TaskDetailVm>($"/api/tasks/{taskId}");
        if (task == null)
        {
            TempData["Error"] = "Không tìm thấy task hoặc bạn không có quyền xem.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Chi tiết task";
        return View(task);
    }

    private async Task<List<AssignmentListVm>> GetAssignmentsAsync()
    {
        return await _api.GetAsync<List<AssignmentListVm>>("/api/internassignments") ?? new();
    }

    private async Task<IActionResult> ReloadEditViewAsync(int id)
    {
        var task = await _api.GetAsync<TaskDetailVm>($"/api/tasks/{id}");
        if (task == null)
            return RedirectToAction(nameof(Index));

        ViewData["Title"] = "Chỉnh sửa task";
        ViewBag.Assignments = await GetAssignmentsAsync();
        return View("EditTask", task);
    }
}
