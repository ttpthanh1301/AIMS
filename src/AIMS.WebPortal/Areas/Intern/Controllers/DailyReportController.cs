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
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public DailyReportController(BackendApiClient api)
        => _api = api;

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private static DateTime GetVietnamToday()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone).Date;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Daily Reports";
        var reports = await _api.GetAsync<List<DailyReportVm>>("/api/dailyreports")
            ?? new List<DailyReportVm>();
        var today = GetVietnamToday();
        ViewBag.TodayDate = today;
        ViewBag.TodayReport = reports.FirstOrDefault(r => r.ReportDate.Date == today);
        return View(reports);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        string content, string? plannedTomorrow, string? issues)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Nội dung báo cáo không được để trống.";
            return RedirectToAction("Index");
        }

        var (success, message) = await _api.PostWithMessageAsync("/api/dailyreports", new
        {
            content = content.Trim(),
            plannedTomorrow = string.IsNullOrWhiteSpace(plannedTomorrow) ? null : plannedTomorrow.Trim(),
            issues = string.IsNullOrWhiteSpace(issues) ? null : issues.Trim()
        });

        TempData[success ? "Success" : "Error"] =
            success ? (message ?? "Đã nộp báo cáo thành công!") : (message ?? "Không thể nộp báo cáo.");

        return RedirectToAction("Index");
    }
}
