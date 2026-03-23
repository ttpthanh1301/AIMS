using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Intern.Controllers;

[Area("Intern")]
[Authorize(Roles = "Intern")]
public class ApplyController : Controller
{
    private readonly BackendApiClient _api;

    public ApplyController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Nộp hồ sơ";
        var jds = await _api.GetAsync<List<dynamic>>(
            "/api/jobdescriptions?status=OPEN") ?? new();
        var myApps = await _api.GetAsync<List<dynamic>>(
            "/api/applications/my") ?? new();
        ViewBag.JDs = jds;
        return View(myApps);
    }

    [HttpPost]
    public async Task<IActionResult> Apply(
        int jobDescriptionId, string? coverLetter, IFormFile cvFile)
    {
        if (cvFile == null)
        {
            TempData["Error"] = "Vui lòng chọn file CV.";
            return RedirectToAction("Index");
        }

        using var ms = new MemoryStream();
        await cvFile.CopyToAsync(ms);
        var fileBytes = ms.ToArray();

        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(fileBytes), "cvFile",
                 cvFile.FileName);
        form.Add(new StringContent(jobDescriptionId.ToString()),
                 "jobDescriptionId");
        if (!string.IsNullOrEmpty(coverLetter))
            form.Add(new StringContent(coverLetter), "coverLetter");

        await _api.PostFormAsync<dynamic>("/api/applications", form);

        TempData["Success"] = "Nộp hồ sơ thành công! AI đang xử lý...";
        return RedirectToAction("Index");
    }
}