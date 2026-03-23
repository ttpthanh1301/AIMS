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
        var url = string.IsNullOrEmpty(internId)
            ? "/api/dailyreports"
            : $"/api/dailyreports?internId={internId}";
        var reports = await _api.GetAsync<List<DailyReportVm>>(url)
            ?? new List<DailyReportVm>();
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

        // ⭐ Thử cả 2 cách lấy data
        TimesheetResultVm? data = null;

        if (!string.IsNullOrEmpty(internId))
        {
            data = await _api.GetAsync<TimesheetResultVm>(
                $"/api/timesheets?internId={internId}");
        }
        else
        {
            // Lấy tất cả intern của mentor
            var assignments = await _api.GetAsync<List<InternAssignmentVm>>(
                "/api/internassignments") ?? new List<InternAssignmentVm>();

            var allItems = new List<TimesheetVm>();
            decimal total = 0;

            // Lấy timesheet từng intern
            foreach (var a in assignments)
            {
                var internData = await _api.GetAsync<TimesheetResultVm>(
                    $"/api/timesheets?internId={a.InternUserId}");

                if (internData?.Items != null)
                {
                    allItems.AddRange(internData.Items);
                    total += internData.TotalHours;
                }
            }

            data = new TimesheetResultVm
            {
                TotalHours = total,
                Items = allItems.OrderByDescending(i => i.WorkDate).ToList(),
            };

            ViewBag.Assignments = assignments;
        }

        // Nếu filter theo intern cụ thể
        if (!string.IsNullOrEmpty(internId))
        {
            var assignments2 = await _api.GetAsync<List<InternAssignmentVm>>(
                "/api/internassignments") ?? new List<InternAssignmentVm>();
            ViewBag.Assignments = assignments2;
        }

        ViewBag.InternId = internId;
        return View(data ?? new TimesheetResultVm());
    }
}