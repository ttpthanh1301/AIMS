namespace AIMS.ViewModels.Systems;

/// <summary>
/// DTO cho ActivityLog - trả về cho frontend
/// </summary>
public class ActivityLogDto
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime CreateDate { get; set; }
    public string? Content { get; set; }

    // Thông tin chi tiết của user (nếu cần)
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }
}

/// <summary>
/// Filter model cho ActivityLog query
/// </summary>
public class ActivityLogFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Summary response cho hoạt động
/// </summary>
public class ActivitySummary
{
    public int TotalActivities { get; set; }
    public int ActiveUsers { get; set; }
    public List<ActionBreakdown> ActionBreakdown { get; set; } = new();
    public List<UserActivity> TopUsers { get; set; } = new();
}

public class ActionBreakdown
{
    public string Action { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UserActivity
{
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public int Count { get; set; }
}
