using AIMS.ViewModels.Systems;
using AIMS.WebPortal.Models.Admin;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AIMS.WebPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly BackendApiClient _api;

    public UserController(BackendApiClient api)
        => _api = api;

    public async Task<IActionResult> Index([FromQuery] UserFilter filter)
    {
        ViewData["Title"] = "Quản lý người dùng";
        filter.PageIndex = Math.Max(filter.PageIndex, 1);
        filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

        var queryParams = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(filter.Keyword)) queryParams["keyword"] = filter.Keyword;
        if (!string.IsNullOrWhiteSpace(filter.Role)) queryParams["role"] = filter.Role;
        if (filter.IsActive.HasValue) queryParams["isActive"] = filter.IsActive.Value.ToString().ToLowerInvariant();
        queryParams["pageIndex"] = filter.PageIndex.ToString();
        queryParams["pageSize"] = filter.PageSize.ToString();

        var url = QueryHelpers.AddQueryString("/api/users", queryParams!);
        var result = await _api.GetAsync<PaginationResult<UserVm>>(url)
            ?? new PaginationResult<UserVm>();

        return View(new AdminUserIndexVm
        {
            Filter = filter,
            Result = result
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Tạo người dùng";
        return View(new CreateUserRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        ViewData["Title"] = "Tạo người dùng";
        if (!ModelState.IsValid)
            return View(request);

        NormalizeRoleSpecificFields(request);

        var result = await _api.PostWithMessageAsync("/api/users", request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể tạo người dùng.");
            return View(request);
        }

        TempData["Success"] = result.Message ?? "Đã tạo người dùng mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _api.GetAsync<UserVm>($"/api/users/{id}");
        if (user == null) return RedirectToIndexWithError("Không tìm thấy người dùng.");

        ViewData["Title"] = "Cập nhật người dùng";
        return View(MapEditVm(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminUserEditVm model)
    {
        ViewData["Title"] = "Cập nhật người dùng";
        if (!ModelState.IsValid)
            return View(model);

        NormalizeRoleSpecificFields(model);

        var updateResult = await _api.PutWithMessageAsync(
            $"/api/users/{model.Id}",
            new UpdateUserRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Avatar = model.Avatar,
                StudentId = model.StudentId,
                GPA = model.GPA,
                IsActive = model.IsActive
            });

        if (!updateResult.Success)
        {
            ModelState.AddModelError(string.Empty, updateResult.Message ?? "Không thể cập nhật người dùng.");
            return View(model);
        }

        var roleResult = await _api.PutWithMessageAsync(
            $"/api/users/{model.Id}/roles",
            new AssignRoleRequest
            {
                Roles = new List<string> { model.SelectedRole }
            });

        if (!roleResult.Success)
        {
            ModelState.AddModelError(string.Empty, roleResult.Message ?? "Không thể cập nhật role.");
            return View(model);
        }

        TempData["Success"] = roleResult.Message ?? updateResult.Message ?? "Đã cập nhật người dùng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        var result = await _api.DeleteWithMessageAsync($"/api/users/{id}");
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? result.Message ?? "Đã vô hiệu hóa người dùng."
            : result.Message ?? "Không thể vô hiệu hóa người dùng.";
        return RedirectToAction(nameof(Index));
    }

    private RedirectToActionResult RedirectToIndexWithError(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    private static AdminUserEditVm MapEditVm(UserVm user)
        => new()
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Avatar = user.Avatar,
            StudentId = user.StudentId,
            GPA = user.GPA,
            IsActive = user.IsActive,
            SelectedRole = user.Roles.FirstOrDefault() ?? "Intern"
        };

    private static void NormalizeRoleSpecificFields(CreateUserRequest request)
    {
        if (!string.Equals(request.Role, "Intern", StringComparison.OrdinalIgnoreCase))
        {
            request.StudentId = null;
            request.GPA = null;
        }
    }

    private static void NormalizeRoleSpecificFields(AdminUserEditVm model)
    {
        if (!string.Equals(model.SelectedRole, "Intern", StringComparison.OrdinalIgnoreCase))
        {
            model.StudentId = null;
            model.GPA = null;
        }
    }
}
