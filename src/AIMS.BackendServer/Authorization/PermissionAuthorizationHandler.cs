using System.IdentityModel.Tokens.Jwt;
using AIMS.BackendServer.Attributes;
using AIMS.BackendServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers; 

namespace AIMS.BackendServer.Authorization;

// ── Requirement (marker) ───────────────────────────────────────
public class PermissionRequirement : IAuthorizationRequirement { }

// ── Handler ────────────────────────────────────────────────────
public class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionCacheService _permissionCache;

    public PermissionAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        IPermissionCacheService permissionCache)
    {
        _httpContextAccessor = httpContextAccessor;
        _permissionCache = permissionCache;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // ── Chưa authenticate → fail ──────────────────────────
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Fail();
            return;
        }

        // ── Admin bypass ──────────────────────────────────────
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        // ── Đọc [RequirePermission] từ Endpoint ───────────────
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Succeed(requirement); // Không có context → cho qua
            return;
        }

        var endpoint = httpContext.GetEndpoint();
        var permAttr = endpoint?
            .Metadata
            .GetMetadata<RequirePermissionAttribute>();

        // Không có attribute → không cần kiểm tra
        if (permAttr == null)
        {
            context.Succeed(requirement);
            return;
        }

        // ── Lấy userId ────────────────────────────────────────
        var userId = context.User
            .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            context.Fail();
            return;
        }

        // ── Kiểm tra permission từ cache ─────────────────────
        var permissions = await _permissionCache
            .GetUserPermissionsAsync(userId);

        var hasPermission = permissions.Contains(
            new PermissionCacheKey(permAttr.FunctionId, permAttr.CommandId));

        if (hasPermission)
            context.Succeed(requirement);
        else
            context.Fail(new AuthorizationFailureReason(
                this,
                $"Thiếu quyền {permAttr.CommandId} trên {permAttr.FunctionId}"));
    }
}