using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIMS.ViewModels.TaskManagement;


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
    ViewData["Title"] = "Tasks của tôi";
    var tasks = await _api.GetAsync<List<TaskVm>>("/api/tasks")
        ?? new List<TaskVm>();
    return View(tasks);
}

[HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        await _api.PutAsync($"/api/tasks/{id}/status", new { status });
        return RedirectToAction("Index");
    }
}