using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.Systems;

// ── Function Response ──────────────────────────────────────────
public class FunctionVm
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public string? ParentId { get; set; }

    // Danh sách Function con (cây menu)
    public List<FunctionVm> Children { get; set; } = new();

    // Commands được phép trong Function này
    public List<CommandVm> Commands { get; set; } = new();
}

// ── Create/Update Function ─────────────────────────────────────
public class CreateFunctionRequest
{
    [Required]
    [StringLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Url { get; set; }

    [StringLength(100)]
    public string? Icon { get; set; }

    public int SortOrder { get; set; } = 1;
    public string? ParentId { get; set; }
}

public class UpdateFunctionRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Url { get; set; }

    [StringLength(100)]
    public string? Icon { get; set; }

    public int SortOrder { get; set; } = 1;
    public string? ParentId { get; set; }
}

// ── Command Response ───────────────────────────────────────────
public class CommandVm
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

// ── Assign Commands to Function ────────────────────────────────
public class AddCommandToFunctionRequest
{
    [Required]
    public string FunctionId { get; set; } = string.Empty;

    [Required]
    public List<string> CommandIds { get; set; } = new();
}