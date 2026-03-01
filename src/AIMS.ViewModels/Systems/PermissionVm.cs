using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.Systems;

// ── Response ───────────────────────────────────────────────────
public class PermissionVm
{
    public string FunctionId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string CommandId { get; set; } = string.Empty;
}

// ── Ma trận hiển thị cho UI ────────────────────────────────────
// Mỗi ô trong ma trận = 1 Function có nhiều CommandPermission
public class PermissionScreenVm
{
    public FunctionVm Function { get; set; } = new();

    // Key = CommandId, Value = có quyền không
    public Dictionary<string, bool> CommandPermissions { get; set; } = new();
}

// ── Batch update permissions cho 1 Role ───────────────────────
public class UpdatePermissionRequest
{
    [Required]
    public string RoleId { get; set; } = string.Empty;

    // Danh sách permission CẦN CÓ sau khi update
    public List<PermissionVm> Permissions { get; set; } = new();
}
