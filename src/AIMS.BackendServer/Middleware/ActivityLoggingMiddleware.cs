using AIMS.BackendServer.Services;
using System.Security.Claims;

namespace AIMS.BackendServer.Middleware;

/// <summary>
/// Middleware để ghi nhật ký hoạt động của người dùng
/// Captures HTTP method, path, và user information
/// </summary>
public class ActivityLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ActivityLoggingMiddleware> _logger;

    // Danh sách các endpoint không cần ghi log (tránh quá tải)
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/validate-token",
        "/api/auth/logout",
        "/health",
        "/swagger",
        "/api/swagger",
        "/scalar"
    };

    public ActivityLoggingMiddleware(RequestDelegate next, ILogger<ActivityLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IActivityLogService activityLogService)
    {
        var originalBodyStream = context.Response.Body;

        // Ghi log cho các request quan trọng
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId) && !IsExcludedPath(context.Request.Path))
        {
            // Lấy method và path
            var method = context.Request.Method;
            var path = context.Request.Path.Value;
            var queryString = context.Request.QueryString.Value;

            try
            {
                // Gọi next middleware
                await _next(context);

                // Chỉ log những request có tác động (POST, PUT, DELETE)
                if (method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH")
                {
                    var statusCode = context.Response.StatusCode;

                    // Chỉ log nếu success (2xx) hoặc không found (404)
                    if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                    {
                        var action = $"{method} {path}";
                        var content = !string.IsNullOrEmpty(queryString) ? $"Query: {queryString}" : null;

                        // Trích xuất entity name từ path (e.g., /api/users/5 -> Users, id: 5)
                        var pathParts = path?.Trim('/').Split('/');
                        var entityName = pathParts?.Length >= 2 ? pathParts[1] : null;
                        var entityId = pathParts?.Length >= 3 ? pathParts[2] : null;

                        await activityLogService.LogActivityAsync(
                            userId,
                            action,
                            entityName,
                            entityId,
                            content
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in activity logging middleware");
                // Không throw lỗi, tiếp tục xử lý request
                throw;
            }
        }
        else
        {
            await _next(context);
        }
    }

    private static bool IsExcludedPath(PathString path)
    {
        var pathValue = path.Value;
        return ExcludedPaths.Any(excluded => pathValue?.Contains(excluded, StringComparison.OrdinalIgnoreCase) == true);
    }
}
