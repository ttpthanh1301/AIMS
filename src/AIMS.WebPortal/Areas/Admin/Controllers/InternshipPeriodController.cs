using AIMS.ViewModels.TaskManagement;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class InternshipPeriodController : Controller
{
    private readonly BackendApiClient _api;

    public InternshipPeriodController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Kỳ thực tập";
        var periods = await _api.GetAsync<List<InternshipPeriodVm>>("/api/internshipperiods")
            ?? new List<InternshipPeriodVm>();
        return View(periods);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Tạo kỳ thực tập";
        return View(new CreateInternshipPeriodRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInternshipPeriodRequest request)
    {
        ViewData["Title"] = "Tạo kỳ thực tập";
        if (!ModelState.IsValid)
            return View(request);

        var result = await _api.PostWithMessageAsync("/api/internshipperiods", request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể tạo kỳ thực tập.");
            return View(request);
        }

        TempData["Success"] = result.Message ?? "Đã tạo kỳ thực tập mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var period = await _api.GetAsync<InternshipPeriodVm>($"/api/internshipperiods/{id}");
        if (period == null)
        {
            TempData["Error"] = "Không tìm thấy kỳ thực tập.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Cập nhật kỳ thực tập";
        return View(new UpdateInternshipPeriodRequest
        {
            Name = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            IsActive = period.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateInternshipPeriodRequest request)
    {
        ViewData["Title"] = "Cập nhật kỳ thực tập";
        if (!ModelState.IsValid)
            return View(request);

        var result = await _api.PutWithMessageAsync($"/api/internshipperiods/{id}", request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể cập nhật kỳ thực tập.");
            return View(request);
        }

        TempData["Success"] = result.Message ?? "Đã cập nhật kỳ thực tập.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _api.PutWithMessageAsync($"/api/internshipperiods/{id}/activate", new { });
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? result.Message ?? "Đã kích hoạt kỳ thực tập."
            : result.Message ?? "Không thể kích hoạt kỳ thực tập.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var result = await _api.PutWithMessageAsync($"/api/internshipperiods/{id}/close", new { });
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? result.Message ?? "Đã đóng kỳ thực tập."
            : result.Message ?? "Không thể đóng kỳ thực tập.";
        return RedirectToAction(nameof(Index));
    }
}
