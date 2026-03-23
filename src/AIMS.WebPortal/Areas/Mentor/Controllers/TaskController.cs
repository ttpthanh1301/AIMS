using AIMS.ViewModels.TaskManagement;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Mentor.Controllers;

[Area("Mentor")]
[Authorize(Roles = "Mentor,Admin")]
public class TaskController : Controller
{
    private readonly BackendApiClient _api;

    public TaskController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Kanban Board";

        // ⭐ Typed ViewModels
        var assignments = await _api.GetAsync<List<AssignmentListVm>>(
            "/api/internassignments") ?? new();
        var tasks = await _api.GetAsync<List<TaskListVm>>(
            "/api/tasks") ?? new();

        ViewBag.Assignments = assignments;
        return View(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        string title, int assignmentId,
        string priority, string deadline,
        decimal? estimatedHours)
    {
        await _api.PostAsync<object>("/api/tasks", new
        {
            title,
            assignmentId,
            priority,
            deadline = DateTime.Parse(deadline),
            estimatedHours,
        });
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        await _api.PutAsync($"/api/tasks/{id}/status", new { status });
        return Ok();
    }
}