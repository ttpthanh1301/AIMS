using AIMS.ViewModels.LMS;
using AIMS.ViewModels.TaskManagement;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Mentor.Controllers;

[Area("Mentor")]
[Authorize(Roles = "Mentor,Admin")]
public class DashboardController : Controller
{
    private readonly BackendApiClient _api;
    public DashboardController(BackendApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Dashboard Mentor";
        var data = await _api.GetAsync<MentorDashboardVm>("/api/dashboard/mentor");

        // ⭐ Lấy daily reports chưa có feedback
        var reports = await _api.GetAsync<List<DailyReportVm>>("/api/dailyreports")
            ?? new List<DailyReportVm>();

        ViewBag.PendingReports = reports.Where(r => !r.HasFeedback).ToList();
        ViewBag.AllReports = reports;

        return View(data);
    }

    // Xem tất cả DailyReport của intern
    public async Task<IActionResult> DailyReports(string? internId = null)
    {
        ViewData["Title"] = "Daily Reports";
        var assignments = await _api.GetAsync<List<InternAssignmentVm>>(
            "/api/internassignments") ?? new List<InternAssignmentVm>();
        var url = string.IsNullOrEmpty(internId)
            ? "/api/dailyreports"
            : $"/api/dailyreports?internId={internId}";
        var reports = await _api.GetAsync<List<DailyReportVm>>(url)
            ?? new List<DailyReportVm>();
        ViewBag.Assignments = assignments;
        ViewBag.InternId = internId;
        return View(reports);
    }

    // Gửi feedback
    [HttpPost]
    public async Task<IActionResult> Feedback(int reportId, string feedback)
    {
        await _api.PutAsync($"/api/dailyreports/{reportId}/feedback",
            new { feedback });
        TempData["Success"] = "Đã gửi phản hồi!";
        return RedirectToAction("DailyReports");
    }

    // Xem Timesheet
    public async Task<IActionResult> Timesheets(string? internId = null)
    {
        ViewData["Title"] = "Timesheet";
        var assignments = await _api.GetAsync<List<InternAssignmentVm>>(
            "/api/internassignments") ?? new List<InternAssignmentVm>();
        var url = string.IsNullOrEmpty(internId)
            ? "/api/timesheets"
            : $"/api/timesheets?internId={internId}";
        var data = await _api.GetAsync<TimesheetResultVm>(url)
            ?? new TimesheetResultVm();

        ViewBag.Assignments = assignments;
        ViewBag.InternId = internId;
        return View(data);
    }
}
