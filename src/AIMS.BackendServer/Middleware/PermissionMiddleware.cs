using AIMS.BackendServer.Services;
using System.Security.Claims;

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
        IPermissionCacheService permissionCache)
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

        // ── Lấy userId — thử tất cả claim types ──────────────
        var userId =
            context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value
            ?? "";

        // Không lấy được userId → cho đi tiếp, [Authorize] sẽ xử lý
        if (string.IsNullOrEmpty(userId))
        {
            await _next(context);
            return;
        }

        // ── Map request → FunctionId + CommandId ─────────────
        var commandId = MapMethodToCommand(context.Request.Method);
        var functionId = MapPathToFunctionId(path);

        // Không map được → cho đi tiếp
        if (string.IsNullOrEmpty(functionId))
        {
            await _next(context);
            return;
        }

        // ── Kiểm tra từ CACHE ─────────────────────────────────
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
            // ── Hệ thống (Admin only) ─────────────────────────
            _ when path.Contains("/api/roles") => "SYSTEM_ROLE",
            _ when path.Contains("/api/users") => "SYSTEM_USER",
            _ when path.Contains("/api/permissions") => "SYSTEM_PERMISSION",
            _ when path.Contains("/api/functions") => "SYSTEM_PERMISSION",

            // ── Tuyển dụng (HR) ───────────────────────────────
            _ when path.Contains("/api/jobdescriptions") => "RECRUITMENT_JD",
            _ when path.Contains("/api/applications") => "RECRUITMENT_CV",
            _ when path.Contains("/api/screening") => "RECRUITMENT_CV",

            // ── LMS (Mentor tạo/sửa) ──────────────────────────
            _ when path.Contains("/api/courses") => "LMS_COURSES",
            _ when path.Contains("/api/lessons") => "LMS_COURSES",
            _ when path.Contains("/api/quizbanks") => "LMS_QUIZ",

            // ── Task (Mentor tạo task) ─────────────────────────
            _ when path.Contains("/api/tasks") => "TASKS_BOARD",

            // ── Daily Report (Mentor xem/feedback) ────────────
            _ when path.Contains("/api/dailyreports") => "TASKS_REPORT",

            // ── Tất cả các endpoint dưới đây dùng [Authorize(Roles)]
            // trong controller tự xử lý → middleware KHÔNG check thêm
            _ when path.Contains("/api/timesheets") => null,  // ← SỬA
            _ when path.Contains("/api/enrollments") => null,
            _ when path.Contains("/api/lessonprogress") => null,
            _ when path.Contains("/api/quizattempts") => null,
            _ when path.Contains("/api/certificates") => null,
            _ when path.Contains("/api/internassignments") => null,
            _ when path.Contains("/api/internshipperiods") => null,
            _ when path.Contains("/api/dashboard") => null,

            _ => null,
        };
    }
    private static bool IsPublicPath(string path) =>
        PublicPaths.Any(p => path.StartsWith(p,
            StringComparison.OrdinalIgnoreCase));
}