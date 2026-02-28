namespace AIMS.BackendServer.Data.Entities;

public class ActivityLog
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public string? Content { get; set; }

    public AppUser User { get; set; } = null!;
}
