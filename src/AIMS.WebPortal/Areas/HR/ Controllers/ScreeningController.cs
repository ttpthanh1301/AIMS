using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = "HR,Admin")]
public class ScreeningController : Controller
{
    private readonly BackendApiClient _api;

    public ScreeningController(BackendApiClient api)
        => _api = api;

    // Ranking List
    public async Task<IActionResult> Ranking(int jdId, int top = 20)
    {
        ViewData["Title"] = "Ranking List — AI Screening";
        var result = await _api.GetAsync<dynamic>(
            $"/api/screening/ranking/{jdId}?top={top}");
        ViewBag.JdId = jdId;
        ViewBag.Result = result;
        return View();
    }

    // Trigger batch screening
    [HttpPost]
    public async Task<IActionResult> RunBatch(int jdId)
    {
        await _api.PostAsync<dynamic>(
            $"/api/screening/batch/{jdId}", new { });
        TempData["Success"] = "Đã chấm điểm xong!";
        return RedirectToAction("Ranking", new { jdId });
    }

    // CV Detail
    public async Task<IActionResult> CVDetail(int applicationId)
    {
        ViewData["Title"] = "Chi tiết CV";
        var app = await _api.GetAsync<dynamic>(
            $"/api/applications/{applicationId}");
        return View(app);
    }

    // Cập nhật status ứng viên
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(
        int applicationId, string status, int jdId)
    {
        await _api.PutAsync(
            $"/api/applications/{applicationId}/status",
            new { status });
        return RedirectToAction("Ranking", new { jdId });
    }
}