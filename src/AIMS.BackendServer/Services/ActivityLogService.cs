using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Services;

public interface IActivityLogService
{
    Task LogActivityAsync(string userId, string action, string? entityName = null, string? entityId = null, string? content = null);
    Task<List<ActivityLog>> GetActivitiesAsync(int page = 1, int pageSize = 20, string? userId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<int> GetTotalCountAsync(string? userId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null);
}

public class ActivityLogService : IActivityLogService
{
    private readonly AimsDbContext _context;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(AimsDbContext context, ILogger<ActivityLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ghi nhật ký hoạt động của người dùng
    /// </summary>
    public async Task LogActivityAsync(string userId, string action, string? entityName = null, string? entityId = null, string? content = null)
    {
        try
        {
            var activityLog = new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Content = content,
                CreateDate = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging activity for user {UserId}: {Action}", userId, action);
            // Không throw lỗi để không ảnh hưởng đến request chính
        }
    }

    /// <summary>
    /// Lấy danh sách nhật ký hoạt động với phân trang và lọc
    /// </summary>
    public async Task<List<ActivityLog>> GetActivitiesAsync(int page = 1, int pageSize = 20, string? userId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.ActivityLogs
            .Include(a => a.User)
            .AsQueryable();

        // Lọc theo userId
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        // Lọc theo action
        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.Contains(action));
        }

        // Lọc theo ngày bắt đầu
        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Date;
            query = query.Where(a => a.CreateDate >= fromUtc);
        }

        // Lọc theo ngày kết thúc
        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreateDate <= toUtc);
        }

        // Sắp xếp theo ngày tạo mới nhất
        query = query.OrderByDescending(a => a.CreateDate);

        // Phân trang
        var skip = (page - 1) * pageSize;
        return await query.Skip(skip).Take(pageSize).ToListAsync();
    }

    /// <summary>
    /// Lấy tổng số lượng nhật ký hoạt động theo điều kiện lọc
    /// </summary>
    public async Task<int> GetTotalCountAsync(string? userId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.ActivityLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.Contains(action));
        }

        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Date;
            query = query.Where(a => a.CreateDate >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreateDate <= toUtc);
        }

        return await query.CountAsync();
    }
}
