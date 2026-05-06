using AIMS.ViewModels.Systems;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ActivityLogController : Controller
{
    private readonly BackendApiClient _api;
    private readonly ILogger<ActivityLogController> _logger;

    public ActivityLogController(BackendApiClient api, ILogger<ActivityLogController> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>
    /// Hiển thị trang danh sách nhật ký hoạt động
    /// </summary>
    public async Task<IActionResult> Index(int page = 1, string? userId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            ViewData["Title"] = "Nhật ký hoạt động";
            ViewData["CurrentPage"] = page;
            ViewData["UserId"] = userId;
            ViewData["Action"] = action;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

            var pageSize = 20;
            var url = $"/api/activitylogs?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(userId))
                url += $"&userId={Uri.EscapeDataString(userId)}";

            if (!string.IsNullOrEmpty(action))
                url += $"&action={Uri.EscapeDataString(action)}";

            if (fromDate.HasValue)
                url += $"&fromDate={fromDate:yyyy-MM-dd}";

            if (toDate.HasValue)
                url += $"&toDate={toDate:yyyy-MM-dd}";

            var response = await _api.GetAsync<dynamic>(url);
            return View(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading activity logs");
            ViewData["ErrorMessage"] = "Lỗi khi tải nhật ký hoạt động";
            return View();
        }
    }

    /// <summary>
    /// Lấy dữ liệu hoạt động theo người dùng (API endpoint)
    /// </summary>
    [HttpGet("user-activities/{userId}")]
    public async Task<IActionResult> UserActivities(string userId, int page = 1)
    {
        try
        {
            var pageSize = 20;
            var url = $"/api/activitylogs/user/{Uri.EscapeDataString(userId)}?page={page}&pageSize={pageSize}";

            var response = await _api.GetAsync<dynamic>(url);
            return PartialView("_UserActivities", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user activities for {UserId}", userId);
            return BadRequest("Lỗi khi tải nhật ký hoạt động");
        }
    }

    /// <summary>
    /// Hiển thị trang thống kê hoạt động
    /// </summary>
    public async Task<IActionResult> Summary(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            ViewData["Title"] = "Thống kê hoạt động";

            var url = "/api/activitylogs/summary";
            if (fromDate.HasValue)
                url += $"?fromDate={fromDate:yyyy-MM-dd}";

            if (toDate.HasValue)
                url += (fromDate.HasValue ? "&" : "?") + $"toDate={toDate:yyyy-MM-dd}";

            var summary = await _api.GetAsync<dynamic>(url);
            return View(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading activity summary");
            ViewData["ErrorMessage"] = "Lỗi khi tải thống kê hoạt động";
            return View();
        }
    }

    /// <summary>
    /// Xuất báo cáo hoạt động (CSV format)
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var url = "/api/activitylogs?page=1&pageSize=10000";

            if (fromDate.HasValue)
                url += $"&fromDate={fromDate:yyyy-MM-dd}";

            if (toDate.HasValue)
                url += $"&toDate={toDate:yyyy-MM-dd}";

            var response = await _api.GetAsync<dynamic>(url);

            // Generate CSV
            var csv = GenerateCsvReport(response);
            var fileName = $"activity-log-{DateTime.Now:yyyy-MM-dd-HHmmss}.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting activity logs");
            return BadRequest("Lỗi khi xuất báo cáo");
        }
    }

    private string GenerateCsvReport(dynamic response)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("\"Mã hành động\",\"Thực thể\",\"Mã thực thể\",\"ID Người dùng\",\"Tên người dùng\",\"Thời gian\",\"Nội dung\"");

        // Assuming response has a Data property with list of activities
        if (response?.Data != null)
        {
            foreach (var item in response.Data)
            {
                csv.AppendLine($"\"{item.Action}\",\"{item.EntityName}\",\"{item.EntityId}\",\"{item.UserId}\",\"{item.UserName}\",\"{item.CreateDate:yyyy-MM-dd HH:mm:ss}\",\"{item.Content}\"");
            }
        }

        return csv.ToString();
    }
}
