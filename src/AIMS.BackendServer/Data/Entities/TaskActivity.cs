namespace AIMS.BackendServer.Data.Entities;

public class TaskActivity
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public TaskItem Task { get; set; } = null!;
    public AppUser ChangedByUser { get; set; } = null!;
}