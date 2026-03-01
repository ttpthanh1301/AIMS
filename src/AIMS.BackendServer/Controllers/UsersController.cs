using AIMS.BackendServer.Data;
using AIMS.BackendServer.Services;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.Systems;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    // ─────────────────────────────────────────────────────────
    // GET /api/users?keyword=&role=&isActive=&pageIndex=1&pageSize=10
    // Lấy danh sách users có phân trang + filter
    // ─────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> GetAll([FromQuery] UserFilter filter)
    {
        // ── Bắt đầu query ─────────────────────────────────────
        var query = _context.Users
            .Include(u => u.University)
            .AsQueryable();

        // ── Filter theo keyword (tìm tên hoặc email) ──────────
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.ToLower().Trim();
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(kw) ||
                (u.Email ?? "").ToLower().Contains(kw) ||
                (u.UserName ?? "").ToLower().Contains(kw));
        }

        // ── Filter theo trạng thái ─────────────────────────────
        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        // ── Filter theo Role ───────────────────────────────────
        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            var role = await _roleManager.FindByNameAsync(filter.Role);
            if (role != null)
            {
                var userIdsInRole = await _context.UserRoles
                    .Where(ur => ur.RoleId == role.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }
        }

        // ── Đếm tổng ──────────────────────────────────────────
        var totalCount = await query.CountAsync();

        // ── Phân trang ────────────────────────────────────────
        var pageSize = Math.Min(filter.PageSize, 50); // Tối đa 50/trang
        var pageIndex = Math.Max(filter.PageIndex, 1);

        var users = await query
            .OrderByDescending(u => u.CreateDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ── Map + gắn Roles cho từng user ─────────────────────
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
            PageSize = pageSize,
        });
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/users/{id}
    // Lấy thông tin 1 user
    // ─────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = $"User '{id}' không tồn tại." });

        var vm = _mapper.Map<UserVm>(user);
        vm.Roles = (await _userManager.GetRolesAsync(user)).ToList();

        return Ok(vm);
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/users
    // Tạo user mới (Admin only)
    // ─────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        // Kiểm tra email đã tồn tại chưa
        if (await _userManager.FindByEmailAsync(request.Email) != null)
            return BadRequest(new { message = $"Email '{request.Email}' đã được sử dụng." });

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
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        // Gán Role
        var validRoles = new[] { "Admin", "HR", "Mentor", "Intern" };
        var roleToAssign = validRoles.Contains(request.Role) ? request.Role : "Intern";
        await _userManager.AddToRoleAsync(user, roleToAssign);

        var vm = _mapper.Map<UserVm>(user);
        vm.Roles = new List<string> { roleToAssign };

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, vm);
    }

    // ─────────────────────────────────────────────────────────
    // PUT /api/users/{id}
    // Cập nhật thông tin user
    // ─────────────────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = $"User '{id}' không tồn tại." });

        // Chỉ Admin hoặc chính user đó mới được sửa
        var currentUserId = User.FindFirst(
            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && currentUserId != id)
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

    // ─────────────────────────────────────────────────────────
    // DELETE /api/users/{id}
    // Xóa user (soft delete — set IsActive = false)
    // ─────────────────────────────────────────────────────────
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id, [FromServices] IPermissionCacheService permissionCache)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = $"User '{id}' không tồn tại." });

        // Không cho xóa chính mình
        var currentUserId = User.FindFirst(
            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (currentUserId == id)
            return BadRequest(new { message = "Không thể xóa tài khoản đang đăng nhập." });

        // Soft delete
        user.IsActive = false;
        user.LastModifiedDate = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        // ⭐ Xóa cache khi user bị deactivate
        permissionCache.InvalidateUser(id);

        return Ok(new { message = $"Đã vô hiệu hóa user '{user.Email}'." });
    }

    // ─────────────────────────────────────────────────────────
    // PUT /api/users/{id}/change-password
    // Đổi mật khẩu
    // ─────────────────────────────────────────────────────────
    [HttpPut("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(
        string id, [FromBody] ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User không tồn tại." });

        var currentUserId = User.FindFirst(
            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (currentUserId != id && !User.IsInRole("Admin"))
            return Forbid();

        var result = await _userManager.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    // ─────────────────────────────────────────────────────────
    // PUT /api/users/{id}/roles
    // Gán roles cho user (Admin only)
    // ─────────────────────────────────────────────────────────
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

        // Lấy roles hiện tại
        var currentRoles = await _userManager.GetRolesAsync(user);

        // Xóa tất cả roles cũ
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Gán roles mới
        var validRoles = new[] { "Admin", "HR", "Mentor", "Intern" };
        var rolesToAssign = request.Roles
            .Where(r => validRoles.Contains(r))
            .ToList();

        if (rolesToAssign.Any())
            await _userManager.AddToRolesAsync(user, rolesToAssign);
        permissionCache.InvalidateUser(id);
        return Ok(new
        {
            message = $"Đã cập nhật roles cho '{user.Email}'.",
            roles = rolesToAssign,
        });
    }
}