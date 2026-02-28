namespace AIMS.BackendServer.Data.Entities;

public class Permission
{
    public string FunctionId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string CommandId { get; set; } = string.Empty;

    public Function Function { get; set; } = null!;
    public AppRole Role { get; set; } = null!;
    public Command Command { get; set; } = null!;
}