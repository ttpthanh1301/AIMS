using System.IdentityModel.Tokens.Jwt;
using AIMS.BackendServer.Data;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Middleware;

public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    // Các path không cần kiểm tra permission
    private static readonly HashSet<string> PublicPaths = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/scalar",
        "/openapi",
        "/health",
    };

    public PermissionMiddleware(RequestDelegate next)
        => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        AimsDbContext db)
    {
        var path = context.Request.Path.Value ?? "";

        // ── 1. Bỏ qua các path public ──────────────────────────
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // ── 2. Bỏ qua nếu chưa đăng nhập (để [Authorize] xử lý) ─
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // ── 3. Admin bypass — không kiểm tra permission ─────────
        if (context.User.IsInRole("Admin"))
        {
            await _next(context);
            return;
        }

        // ── 4. Lấy userId từ JWT claim ──────────────────────────
        var userId = context.User
            .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(
                new { message = "Không xác định được người dùng." });
            return;
        }

        // ── 5. Map HTTP Method → CommandId ─────────────────────
        var commandId = context.Request.Method.ToUpper() switch
        {
            "GET" => "VIEW",
            "POST" => "CREATE",
            "PUT" => "UPDATE",
            "PATCH" => "UPDATE",
            "DELETE" => "DELETE",
            _ => "VIEW",
        };

        // ── 6. Map API Path → FunctionId ───────────────────────
        var functionId = MapPathToFunctionId(path);

        // Không map được → cho phép qua (các API nội bộ)
        if (string.IsNullOrEmpty(functionId))
        {
            await _next(context);
            return;
        }

        // ── 7. Kiểm tra Permission trong DB ────────────────────
        var roleIds = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var hasPermission = await db.Permissions
            .AnyAsync(p =>
                roleIds.Contains(p.RoleId) &&
                p.FunctionId == functionId &&
                p.CommandId == commandId);

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

    // ─────────────────────────────────────────────────────────
    // Map URL path → FunctionId
    // ─────────────────────────────────────────────────────────
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
            _ when path.Contains("/api/lessons") => "LMS_COURSES",
            _ when path.Contains("/api/quizbanks") => "LMS_QUIZ",
            _ when path.Contains("/api/quizattempts") => "LMS_QUIZ",
            _ when path.Contains("/api/certificates") => "LMS_CERTIFICATE",

            _ when path.Contains("/api/tasks") => "TASKS_BOARD",
            _ when path.Contains("/api/dailyreports") => "TASKS_REPORT",
            _ when path.Contains("/api/timesheets") => "TASKS_TIMESHEET",

            _ => null  // Không map → cho phép qua
        };
    }

    private static bool IsPublicPath(string path)
        => PublicPaths.Any(p => path.StartsWith(p,
            StringComparison.OrdinalIgnoreCase));
}