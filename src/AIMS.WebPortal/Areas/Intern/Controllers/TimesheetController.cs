using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIMS.ViewModels.TaskManagement;

namespace AIMS.WebPortal.Areas.Intern.Controllers;

[Area("Intern")]
[Authorize(Roles = "Intern")]
public class TimesheetController : Controller
{
    private readonly BackendApiClient _api;

    public TimesheetController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Timesheet";
        var data = await _api.GetAsync<TimesheetResultVm>("/api/timesheets");
        var tasks = await _api.GetAsync<List<TaskVm>>("/api/tasks")
            ?? new List<TaskVm>();
        ViewBag.Tasks = tasks;
        return View(data);
    }

    [HttpPost]
    public async Task<IActionResult> Log(
        int taskId, decimal hoursWorked, string? workNote)
    {
        var ok = await _api.PostAsync<dynamic>("/api/timesheets",
            new { taskId, hoursWorked, workNote });
        TempData[ok != null ? "Success" : "Error"] =
            ok != null ? "Log giờ thành công!" : "Lỗi khi log giờ.";
        return RedirectToAction("Index");
    }
}