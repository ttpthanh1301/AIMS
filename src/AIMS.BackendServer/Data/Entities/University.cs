namespace AIMS.BackendServer.Data.Entities;

public class University
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}