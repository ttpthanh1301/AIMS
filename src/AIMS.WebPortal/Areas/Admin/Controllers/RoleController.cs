using AIMS.ViewModels.Systems;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RoleController : Controller
{
    private readonly BackendApiClient _api;

    public RoleController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý roles";
        var roles = await _api.GetAsync<List<RoleVm>>("/api/roles")
            ?? new List<RoleVm>();
        return View(roles);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Tạo role";
        return View(new CreateRoleRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleRequest request)
    {
        ViewData["Title"] = "Tạo role";
        if (!ModelState.IsValid)
            return View(request);

        var result = await _api.PostWithMessageAsync("/api/roles", request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể tạo role.");
            return View(request);
        }

        TempData["Success"] = result.Message ?? "Đã tạo role mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await _api.GetAsync<RoleVm>($"/api/roles/{id}");
        if (role == null)
        {
            TempData["Error"] = "Không tìm thấy role.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Cập nhật role";
        return View(new UpdateRoleRequest
        {
            Name = role.Name,
            Description = role.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UpdateRoleRequest request)
    {
        ViewData["Title"] = "Cập nhật role";
        if (!ModelState.IsValid)
            return View(request);

        var result = await _api.PutWithMessageAsync($"/api/roles/{id}", request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể cập nhật role.");
            return View(request);
        }

        TempData["Success"] = result.Message ?? "Đã cập nhật role.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _api.DeleteWithMessageAsync($"/api/roles/{id}");
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? result.Message ?? "Đã xóa role."
            : result.Message ?? "Không thể xóa role.";
        return RedirectToAction(nameof(Index));
    }
}
