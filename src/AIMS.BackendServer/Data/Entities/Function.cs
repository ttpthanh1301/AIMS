namespace AIMS.BackendServer.Data.Entities;

public class Function
{
    public string Id { get; set; } = string.Empty;   // VD: "RECRUITMENT"
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public string? ParentId { get; set; }

    public Function? Parent { get; set; }
    public ICollection<Function> Children { get; set; } = new List<Function>();
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}