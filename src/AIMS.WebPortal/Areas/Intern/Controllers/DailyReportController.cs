using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIMS.ViewModels.TaskManagement;

namespace AIMS.WebPortal.Areas.Intern.Controllers;

[Area("Intern")]
[Authorize(Roles = "Intern")]
public class DailyReportController : Controller
{
    private readonly BackendApiClient _api;

    public DailyReportController(BackendApiClient api)
        => _api = api;
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Daily Reports";
        var reports = await _api.GetAsync<List<DailyReportVm>>("/api/dailyreports")
            ?? new List<DailyReportVm>();
        return View(reports);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        string content, string? plannedTomorrow, string? issues)
    {
        await _api.PostAsync<dynamic>("/api/dailyreports", new
        {
            content,
            plannedTomorrow,
            issues
        });
        TempData["Success"] = "Đã nộp báo cáo thành công!";
        return RedirectToAction("Index");
    }
}