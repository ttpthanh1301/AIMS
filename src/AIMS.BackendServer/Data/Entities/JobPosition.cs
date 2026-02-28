namespace AIMS.BackendServer.Data.Entities;

public class JobPosition
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    public ICollection<JobDescription> JobDescriptions { get; set; } = new List<JobDescription>();
}