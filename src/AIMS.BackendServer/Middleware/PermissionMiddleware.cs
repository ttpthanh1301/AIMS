using System.IdentityModel.Tokens.Jwt;
using AIMS.BackendServer.Services;

namespace AIMS.BackendServer.Middleware;

public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> PublicPaths = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/scalar",
        "/openapi",
        "/health",
    };

    public PermissionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IPermissionCacheService permissionCache)  // ← Inject cache service
    {
        var path = context.Request.Path.Value ?? "";

        // ── Public path → skip ────────────────────────────────
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // ── Chưa login → skip (để [Authorize] xử lý) ─────────
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // ── Admin bypass ──────────────────────────────────────
        if (context.User.IsInRole("Admin"))
        {
            await _next(context);
            return;
        }

        var userId = context.User
            .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(
                new { message = "Không xác định được người dùng." });
            return;
        }

        // ── Map request → FunctionId + CommandId ─────────────
        var commandId = MapMethodToCommand(context.Request.Method);
        var functionId = MapPathToFunctionId(path);

        if (string.IsNullOrEmpty(functionId))
        {
            await _next(context);
            return;
        }

        // ── Kiểm tra từ CACHE (không query DB) ───────────────
        var permissions = await permissionCache
            .GetUserPermissionsAsync(userId);

        var hasPermission = permissions.Contains(
            new PermissionCacheKey(functionId, commandId));

        if (!hasPermission)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Bạn không có quyền thực hiện thao tác này.",
                functionId = functionId,
                commandId = commandId,
            });
            return;
        }

        await _next(context);
    }

    private static string MapMethodToCommand(string method) =>
        method.ToUpper() switch
        {
            "GET" => "VIEW",
            "POST" => "CREATE",
            "PUT" => "UPDATE",
            "PATCH" => "UPDATE",
            "DELETE" => "DELETE",
            _ => "VIEW",
        };

    private static string? MapPathToFunctionId(string path)
    {
        path = path.ToLower();
        return path switch
        {
            _ when path.Contains("/api/roles") => "SYSTEM_ROLE",
            _ when path.Contains("/api/users") => "SYSTEM_USER",
            _ when path.Contains("/api/permissions") => "SYSTEM_PERMISSION",
            _ when path.Contains("/api/functions") => "SYSTEM_PERMISSION",
            _ when path.Contains("/api/jobdescriptions") => "RECRUITMENT_JD",
            _ when path.Contains("/api/applications") => "RECRUITMENT_CV",
            _ when path.Contains("/api/screening") => "RECRUITMENT_CV",
            _ when path.Contains("/api/courses") => "LMS_COURSES",
            _ when path.Contains("/api/quizbanks") => "LMS_QUIZ",
            _ when path.Contains("/api/certificates") => "LMS_CERTIFICATE",
            _ when path.Contains("/api/tasks") => "TASKS_BOARD",
            _ when path.Contains("/api/dailyreports") => "TASKS_REPORT",
            _ when path.Contains("/api/timesheets") => "TASKS_TIMESHEET",
            _ => null,
        };
    }

    private static bool IsPublicPath(string path) =>
        PublicPaths.Any(p => path.StartsWith(p,
            StringComparison.OrdinalIgnoreCase));
}