using AIMS.ViewModels.Systems;
using AIMS.ViewModels.TaskManagement;
using AIMS.WebPortal.Models.Admin;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AIMS.WebPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class InternAssignmentController : Controller
{
    private readonly BackendApiClient _api;

    public InternAssignmentController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index(int? periodId = null)
    {
        ViewData["Title"] = "Phân công intern";
        return View(await BuildIndexVm(periodId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInternAssignmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu phân công chưa hợp lệ.";
            return View("Index", await BuildIndexVm(request.PeriodId));
        }

        var result = await _api.PostWithMessageAsync("/api/internassignments", request);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? result.Message ?? "Đã phân công intern cho mentor."
            : result.Message ?? "Không thể tạo phân công.";

        return RedirectToAction(nameof(Index), new { periodId = request.PeriodId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? periodId = null)
    {
        var result = await _api.DeleteWithMessageAsync($"/api/internassignments/{id}");
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? result.Message ?? "Đã hủy phân công."
            : result.Message ?? "Không thể hủy phân công.";

        return RedirectToAction(nameof(Index), new { periodId });
    }

    private async Task<AdminAssignmentIndexVm> BuildIndexVm(int? periodId)
    {
        var assignmentsUrl = periodId.HasValue
            ? QueryHelpers.AddQueryString("/api/internassignments", new Dictionary<string, string?> { ["periodId"] = periodId.Value.ToString() })
            : "/api/internassignments";

        var assignments = await _api.GetAsync<List<InternAssignmentVm>>(assignmentsUrl)
            ?? new List<InternAssignmentVm>();
        var periods = await _api.GetAsync<List<InternshipPeriodVm>>("/api/internshipperiods")
            ?? new List<InternshipPeriodVm>();
        var interns = (await _api.GetAsync<PaginationResult<UserVm>>(
            QueryHelpers.AddQueryString("/api/users", new Dictionary<string, string?> {
                ["role"] = "Intern",
                ["isActive"] = "true",
                ["pageIndex"] = "1",
                ["pageSize"] = "100"
            })))?.Items ?? new List<UserVm>();
        var mentors = (await _api.GetAsync<PaginationResult<UserVm>>(
            QueryHelpers.AddQueryString("/api/users", new Dictionary<string, string?> {
                ["role"] = "Mentor",
                ["isActive"] = "true",
                ["pageIndex"] = "1",
                ["pageSize"] = "100"
            })))?.Items ?? new List<UserVm>();

        return new AdminAssignmentIndexVm
        {
            Assignments = assignments,
            Periods = periods,
            Interns = interns,
            Mentors = mentors,
            PeriodId = periodId
        };
    }
}
