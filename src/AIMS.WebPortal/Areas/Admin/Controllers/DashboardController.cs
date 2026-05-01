using AIMS.WebPortal.Models.Admin;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly BackendApiClient _api;

    public DashboardController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Admin Dashboard";
        var model = await _api.GetAsync<AdminDashboardVm>("/api/dashboard/admin")
            ?? new AdminDashboardVm();
        return View(model);
    }
}
