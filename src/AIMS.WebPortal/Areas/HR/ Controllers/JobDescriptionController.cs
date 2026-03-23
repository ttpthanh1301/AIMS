using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = "HR,Admin")]
public class JobDescriptionController : Controller
{
    private readonly BackendApiClient _api;

    public JobDescriptionController(BackendApiClient api)
        => _api = api;

    // Danh sách JD
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Job Descriptions";
        var jds = await _api.GetAsync<List<dynamic>>(
            "/api/jobdescriptions") ?? new();
        return View(jds);
    }

    // Form tạo JD
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Tạo Job Description";
        var positions = await _api.GetAsync<List<dynamic>>(
            "/api/jobpositions") ?? new();
        ViewBag.Positions = positions;
        return View();
    }

    // Submit tạo JD
    [HttpPost]
    public async Task<IActionResult> Create(
        int jobPositionId, string title,
        string detailContent, string requiredSkills,
        decimal? minGPA, string? deadlineDate)
    {
        var body = new
        {
            jobPositionId,
            title,
            detailContent,
            requiredSkills,
            minGPA,
            deadlineDate = string.IsNullOrEmpty(deadlineDate)
                ? (DateTime?)null
                : DateTime.Parse(deadlineDate),
        };

        await _api.PostAsync<dynamic>("/api/jobdescriptions", body);
        TempData["Success"] = "Tạo JD thành công!";
        return RedirectToAction("Index");
    }
}