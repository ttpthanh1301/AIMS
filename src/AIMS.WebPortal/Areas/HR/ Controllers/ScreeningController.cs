using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AIMS.WebPortal.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = "HR,Admin")]
public class ScreeningController : Controller
{
    private readonly BackendApiClient _api;

    public ScreeningController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "AI Screening";
        var jds = await _api.GetAsync<List<JsonElement>>("/api/jobdescriptions") ?? new();
        return View(jds);
    }

    // Ranking List
    public async Task<IActionResult> Ranking(int jdId, int top = 20)
    {
        ViewData["Title"] = "Ranking List — AI Screening";
        var result = await _api.GetAsync<JsonElement>(
            $"/api/screening/ranking/{jdId}?top={top}");
        ViewBag.JdId = jdId;
        ViewBag.Result = result;
        return View();
    }

    // Trigger batch screening
    [HttpPost]
    public async Task<IActionResult> RunBatch(int jdId)
    {
        var result = await _api.PostWithMessageAsync(
            $"/api/screening/batch/{jdId}", new { });
        TempData[result.Success ? "Success" : "Error"] =
            result.Message ?? (result.Success ? "Đã chấm điểm xong!" : "Không thể chạy AI Screening.");
        return RedirectToAction("Ranking", new { jdId });
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> UploadBatch(int jdId, List<IFormFile> cvFiles)
    {
        if (cvFiles == null || cvFiles.Count == 0)
        {
            TempData["Error"] = "Vui lòng chọn ít nhất một file PDF.";
            return RedirectToAction("Ranking", new { jdId });
        }

        using var form = new MultipartFormDataContent();
        foreach (var file in cvFiles)
        {
            var content = new StreamContent(file.OpenReadStream());
            content.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType);
            form.Add(content, "cvFiles", file.FileName);
        }

        var result = await _api.PostFormWithMessageAsync<JsonElement>(
            $"/api/screening/upload/{jdId}", form);
        var success = result.Data.ValueKind != JsonValueKind.Undefined;
        TempData[success ? "Success" : "Error"] =
            result.Message ?? (success ? "Đã upload và xử lý CV." : "Không thể upload CV.");

        return RedirectToAction("Ranking", new { jdId });
    }

    // CV Detail
    public async Task<IActionResult> CVDetail(int applicationId)
    {
        ViewData["Title"] = "Chi tiết CV";
        var app = await _api.GetAsync<JsonElement>(
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

    // Delete Application
    [HttpPost]
    public async Task<IActionResult> DeleteApplication(int applicationId, int jdId)
    {
        var result = await _api.DeleteWithMessageAsync(
            $"/api/applications/{applicationId}");
        TempData[result.Success ? "Success" : "Error"] =
            result.Message ?? (result.Success ? "Đã xóa ứng viên." : "Không thể xóa ứng viên.");
        return RedirectToAction("Ranking", new { jdId });
    }
}
