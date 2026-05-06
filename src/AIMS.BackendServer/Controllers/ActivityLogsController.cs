using AIMS.BackendServer.Services;
using AIMS.ViewModels.Systems;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ActivityLogsController : ControllerBase
{
    private readonly IActivityLogService _activityLogService;
    private readonly IMapper _mapper;
    private readonly ILogger<ActivityLogsController> _logger;

    public ActivityLogsController(
        IActivityLogService activityLogService,
        IMapper mapper,
        ILogger<ActivityLogsController> logger)
    {
        _activityLogService = activityLogService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// GET: /api/activitylogs
    /// Lấy danh sách nhật ký hoạt động (chỉ Admin có quyền)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetActivityLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var activities = await _activityLogService.GetActivitiesAsync(
                page, pageSize, userId, action, fromDate, toDate);

            var totalCount = await _activityLogService.GetTotalCountAsync(
                userId, action, fromDate, toDate);

            var result = new PaginatedResponse<ActivityLogDto>
            {
                Data = _mapper.Map<List<ActivityLogDto>>(activities),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (totalCount + pageSize - 1) / pageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity logs");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi khi lấy nhật ký hoạt động" });
        }
    }

    /// <summary>
    /// GET: /api/activitylogs/user/{userId}
    /// Lấy nhật ký hoạt động của một người dùng cụ thể
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserActivityLogs(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "UserId không hợp lệ" });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var activities = await _activityLogService.GetActivitiesAsync(
                page, pageSize, userId, null, fromDate, toDate);

            var totalCount = await _activityLogService.GetTotalCountAsync(
                userId, null, fromDate, toDate);

            var result = new PaginatedResponse<ActivityLogDto>
            {
                Data = _mapper.Map<List<ActivityLogDto>>(activities),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (totalCount + pageSize - 1) / pageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity logs for {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi khi lấy nhật ký hoạt động của người dùng" });
        }
    }

    /// <summary>
    /// GET: /api/activitylogs/summary
    /// Lấy thống kê tóm tắt hoạt động
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetActivitySummary(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var activities = await _activityLogService.GetActivitiesAsync(
                1, int.MaxValue, null, null, fromDate, toDate);

            var summary = new
            {
                TotalActivities = activities.Count,
                ActiveUsers = activities.Select(a => a.UserId).Distinct().Count(),
                ActionBreakdown = activities
                    .GroupBy(a => a.Action)
                    .Select(g => new { action = g.Key, count = g.Count() })
                    .OrderByDescending(x => x.count)
                    .ToList(),
                TopUsers = activities
                    .GroupBy(a => a.UserId)
                    .Select(g => new
                    {
                        userId = g.Key,
                        userName = g.First().User?.UserName,
                        count = g.Count()
                    })
                    .OrderByDescending(x => x.count)
                    .Take(10)
                    .ToList()
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity summary");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi khi lấy thống kê hoạt động" });
        }
    }

    /// <summary>
    /// GET: /api/activitylogs/my-activities
    /// Lấy nhật ký hoạt động của chính người dùng hiện tại
    /// </summary>
    [HttpGet("my-activities")]
    public async Task<IActionResult> GetMyActivities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không xác định được người dùng" });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var activities = await _activityLogService.GetActivitiesAsync(
                page, pageSize, userId);

            var totalCount = await _activityLogService.GetTotalCountAsync(userId);

            var result = new PaginatedResponse<ActivityLogDto>
            {
                Data = _mapper.Map<List<ActivityLogDto>>(activities),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (totalCount + pageSize - 1) / pageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user activity logs");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi khi lấy nhật ký hoạt động của bạn" });
        }
    }
}

/// <summary>
/// Response model cho danh sách phân trang
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
