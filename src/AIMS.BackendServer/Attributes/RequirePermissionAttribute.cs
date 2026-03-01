namespace AIMS.BackendServer.Attributes;

/// <summary>
/// Khai báo permission cần thiết ngay trên Controller/Action.
/// Thay thế string matching trong Middleware.
///
/// Ví dụ:
/// [RequirePermission("RECRUITMENT_JD", "CREATE")]
/// public async Task<IActionResult> Create(...) { }
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false)]
public class RequirePermissionAttribute : Attribute
{
    public string FunctionId { get; }
    public string CommandId { get; }

    public RequirePermissionAttribute(string functionId, string commandId)
    {
        FunctionId = functionId;
        CommandId = commandId;
    }
}