using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.Systems;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIMS.BackendServer.Services;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class PermissionsController : ControllerBase
{
    private readonly AimsDbContext _context;
    private readonly IMapper _mapper;

    public PermissionsController(AimsDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/permissions/{roleId}
    // Lấy tất cả permissions của 1 Role
    // ─────────────────────────────────────────────────────────
    [HttpGet("{roleId}")]
    public async Task<IActionResult> GetByRole(string roleId)
    {
        var permissions = await _context.Permissions
            .Where(p => p.RoleId == roleId)
            .Select(p => new PermissionVm
            {
                FunctionId = p.FunctionId,
                RoleId = p.RoleId,
                CommandId = p.CommandId,
            })
            .ToListAsync();

        return Ok(permissions);
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/permissions/{roleId}/screen
    // Lấy ma trận phân quyền cho UI (Function x Command)
    // ─────────────────────────────────────────────────────────
    [HttpGet("{roleId}/screen")]
    public async Task<IActionResult> GetPermissionScreen(string roleId)
    {
        // Lấy tất cả Functions
        var functions = await _context.Functions
            .Include(f => f.Children)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();

        // Lấy tất cả Commands
        var commands = await _context.Commands
            .OrderBy(c => c.Id)
            .ToListAsync();

        // Lấy permissions hiện tại của Role
        var currentPermissions = await _context.Permissions
            .Where(p => p.RoleId == roleId)
            .Select(p => new { p.FunctionId, p.CommandId })
            .ToListAsync();

        // Build ma trận
        var screen = functions.Select(f => new PermissionScreenVm
        {
            Function = _mapper.Map<FunctionVm>(f),
            CommandPermissions = commands.ToDictionary(
                c => c.Id,
                c => currentPermissions.Any(p =>
                    p.FunctionId == f.Id && p.CommandId == c.Id)
            ),
        }).ToList();

        return Ok(new
        {
            RoleId = roleId,
            Commands = _mapper.Map<List<CommandVm>>(commands),
            Screen = screen,
        });
    }

    // ─────────────────────────────────────────────────────────
    // PUT /api/permissions
    // Batch update toàn bộ permissions của 1 Role
    // (UI gửi lên danh sách permissions MỚI — replace hoàn toàn)
    // ─────────────────────────────────────────────────────────
    [HttpPut]
    public async Task<IActionResult> Update(
    [FromBody] UpdatePermissionRequest request
    , [FromServices] IPermissionCacheService permissionCache)
    {
        // Xóa toàn bộ permissions cũ của Role này
        var oldPermissions = _context.Permissions
            .Where(p => p.RoleId == request.RoleId);
        _context.Permissions.RemoveRange(oldPermissions);

        // Thêm permissions mới
        if (request.Permissions.Any())
        {
            var newPermissions = request.Permissions
                .Select(p => new Permission
                {
                    RoleId = request.RoleId,
                    FunctionId = p.FunctionId,
                    CommandId = p.CommandId,
                })
                .ToList();

            await _context.Permissions.AddRangeAsync(newPermissions);
        }

        await _context.SaveChangesAsync();

        // ⭐ Invalidate cache TẤT CẢ users vì thay đổi permission của role
        // ảnh hưởng đến tất cả users có role đó
        permissionCache.InvalidateAll();
        return Ok(new
        {
            message = $"Đã cập nhật {request.Permissions.Count} permissions.",
        });
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/permissions/user/{userId}
    // Lấy tất cả permissions của 1 User (qua các Roles)
    // Dùng cho JWT Middleware kiểm tra quyền động
    // ─────────────────────────────────────────────────────────
    [HttpGet("user/{userId}")]
    [Authorize] // Tất cả user đều gọi được endpoint này
    public async Task<IActionResult> GetByUser(string userId)
    {
        // Lấy roleIds của user
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        // Lấy tất cả permissions của những roles đó
        var permissions = await _context.Permissions
            .Where(p => roleIds.Contains(p.RoleId))
            .Select(p => new PermissionVm
            {
                FunctionId = p.FunctionId,
                RoleId = p.RoleId,
                CommandId = p.CommandId,
            })
            .Distinct()
            .ToListAsync();

        return Ok(permissions);
    }
}

