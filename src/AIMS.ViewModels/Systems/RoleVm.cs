using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.Systems;

// ── Response ──────────────────────────────────────────────────
public class RoleVm
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ── Create Request ────────────────────────────────────────────
public class CreateRoleRequest
{
    [Required(ErrorMessage = "Id không được để trống")]
    [StringLength(50, MinimumLength = 2)]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên role không được để trống")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

// ── Update Request ────────────────────────────────────────────
public class UpdateRoleRequest
{
    [Required(ErrorMessage = "Tên role không được để trống")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}