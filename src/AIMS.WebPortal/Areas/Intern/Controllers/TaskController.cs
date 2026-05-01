using AIMS.ViewModels.TaskManagement;
using AIMS.WebPortal.Models;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Intern.Controllers;

[Area("Intern")]
[Authorize(Roles = "Intern")]
public class TaskController : Controller
{
    private readonly BackendApiClient _api;

    public TaskController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Kanban Board";
        var tasks = await _api.GetAsync<List<TaskVm>>("/api/tasks")
            ?? new List<TaskVm>();
        return View(tasks);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (id <= 0)
            return RedirectToAction(nameof(Index));

        var task = await _api.GetAsync<TaskDetailVm>($"/api/tasks/{id}");
        if (task == null)
        {
            TempData["Error"] = "Không tìm thấy task hoặc bạn không có quyền xem.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Chi tiết task";
        return View(task);
    }

    [HttpPost]
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
                message = "Cập nhật trạng thái thành công",
                newStatus = request.Status.ToUpper()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
        }
    }
}
