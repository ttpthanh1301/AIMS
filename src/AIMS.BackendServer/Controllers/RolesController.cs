using AIMS.BackendServer.Data;
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
[Authorize(Roles = "Admin")]  // Chỉ Admin mới được quản lý Roles
public class RolesController : ControllerBase
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IMapper _mapper;

    public RolesController(RoleManager<AppRole> roleManager, IMapper mapper)
    {
        _roleManager = roleManager;
        _mapper = mapper;
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/roles
    // Lấy danh sách tất cả roles
    // ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();

        var result = _mapper.Map<List<RoleVm>>(roles);
        return Ok(result);
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/roles/{id}
    // Lấy thông tin 1 role theo Id
    // ─────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound(new { message = $"Role '{id}' không tồn tại." });

        return Ok(_mapper.Map<RoleVm>(role));
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/roles
    // Tạo role mới
    // ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        // Kiểm tra Id đã tồn tại chưa
        if (await _roleManager.RoleExistsAsync(request.Name))
            return BadRequest(new { message = $"Role '{request.Name}' đã tồn tại." });

        var role = new AppRole
        {
            Id = request.Id.ToUpper(),
            Name = request.Name,
            Description = request.Description,
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(
            nameof(GetById),
            new { id = role.Id },
            _mapper.Map<RoleVm>(role));
    }

    // ─────────────────────────────────────────────────────────
    // PUT /api/roles/{id}
    // Cập nhật role
    // ─────────────────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound(new { message = $"Role '{id}' không tồn tại." });

        // Không cho phép sửa các role hệ thống
        var systemRoles = new[] { "Admin", "HR", "Mentor", "Intern" };
        if (systemRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "Không thể sửa role hệ thống." });

        role.Name = request.Name;
        role.Description = request.Description;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(_mapper.Map<RoleVm>(role));
    }

    // ─────────────────────────────────────────────────────────
    // DELETE /api/roles/{id}
    // Xóa role
    // ─────────────────────────────────────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound(new { message = $"Role '{id}' không tồn tại." });

        // Không cho phép xóa các role hệ thống
        var systemRoles = new[] { "Admin", "HR", "Mentor", "Intern" };
        if (systemRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "Không thể xóa role hệ thống." });

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = $"Đã xóa role '{role.Name}' thành công." });
    }
}