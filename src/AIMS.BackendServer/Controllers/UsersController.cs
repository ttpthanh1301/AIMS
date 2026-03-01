using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.Services;
using AIMS.ViewModels.Systems;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly AimsDbContext _context;
    private readonly IMapper _mapper;

    public UsersController(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        AimsDbContext context,
        IMapper mapper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _mapper = mapper;
    }

    // Helper lấy UserId chuẩn Identity
    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    // ─────────────────────────────────────────────
    // GET /api/users
    // ─────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> GetAll([FromQuery] UserFilter filter)
    {
        var query = _context.Users
            .Include(u => u.University)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim().ToLower();
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(kw) ||
                (u.Email ?? "").ToLower().Contains(kw) ||
                (u.UserName ?? "").ToLower().Contains(kw));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            var role = await _roleManager.FindByNameAsync(filter.Role);
            if (role != null)
            {
                query = from u in query
                        join ur in _context.UserRoles on u.Id equals ur.UserId
                        where ur.RoleId == role.Id
                        select u;
            }
        }

        var totalCount = await query.CountAsync();

        var pageSize = Math.Clamp(filter.PageSize, 1, 50);
        var pageIndex = Math.Max(filter.PageIndex, 1);

        var users = await query
            .OrderByDescending(u => u.CreateDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userVms = new List<UserVm>();

        foreach (var user in users)
        {
            var vm = _mapper.Map<UserVm>(user);
            vm.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            userVms.Add(vm);
        }

        return Ok(new PaginationResult<UserVm>
        {
            Items = userVms,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        });
    }

    // ─────────────────────────────────────────────
    // GET by Id
    // ─────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userManager.Users
            .Include(u => u.University)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound(new { message = $"User '{id}' không tồn tại." });

        var vm = _mapper.Map<UserVm>(user);
        vm.Roles = (await _userManager.GetRolesAsync(user)).ToList();

        return Ok(vm);
    }

    // ─────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) != null)
            return BadRequest(new { message = "Email đã được sử dụng." });

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            StudentId = request.StudentId,
            GPA = request.GPA,
            IsActive = true,
            EmailConfirmed = true,
            CreateDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var validRoles = new[] { "Admin", "HR", "Mentor", "Intern" };
        var roleToAssign = validRoles.Contains(request.Role)
            ? request.Role
            : "Intern";

        await _userManager.AddToRoleAsync(user, roleToAssign);

        var vm = _mapper.Map<UserVm>(user);
        vm.Roles = new List<string> { roleToAssign };

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, vm);
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User không tồn tại." });

        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && CurrentUserId != id)
            return Forbid();

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.Avatar = request.Avatar;
        user.StudentId = request.StudentId;
        user.GPA = request.GPA;
        user.IsActive = request.IsActive;
        user.LastModifiedDate = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var vm = _mapper.Map<UserVm>(user);
        vm.Roles = (await _userManager.GetRolesAsync(user)).ToList();

        return Ok(vm);
    }

    // ─────────────────────────────────────────────
    // DELETE (Soft)
    // ─────────────────────────────────────────────
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(
        string id,
        [FromServices] IPermissionCacheService permissionCache)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User không tồn tại." });

        if (CurrentUserId == id)
            return BadRequest(new { message = "Không thể xóa tài khoản đang đăng nhập." });

        user.IsActive = false;
        user.LastModifiedDate = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        permissionCache.InvalidateUser(id);

        return Ok(new { message = $"Đã vô hiệu hóa user '{user.Email}'." });
    }

    // ─────────────────────────────────────────────
    // CHANGE PASSWORD
    // ─────────────────────────────────────────────
    [HttpPut("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(
        string id,
        [FromBody] ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User không tồn tại." });

        if (CurrentUserId != id && !User.IsInRole("Admin"))
            return Forbid();

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    // ─────────────────────────────────────────────
    // ASSIGN ROLES
    // ─────────────────────────────────────────────
    [HttpPut("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRoles(
        string id,
        [FromBody] AssignRoleRequest request,
        [FromServices] IPermissionCacheService permissionCache)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User không tồn tại." });

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        var validRoles = new[] { "Admin", "HR", "Mentor", "Intern" };

        var rolesToAssign = request.Roles
            .Where(r => validRoles.Contains(r))
            .ToList();

        if (rolesToAssign.Any())
            await _userManager.AddToRolesAsync(user, rolesToAssign);

        permissionCache.InvalidateUser(id);

        return Ok(new
        {
            message = "Cập nhật roles thành công.",
            roles = rolesToAssign
        });
    }
}