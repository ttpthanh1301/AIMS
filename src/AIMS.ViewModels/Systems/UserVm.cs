using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.Systems;

// ── Response ──────────────────────────────────────────────────
public class UserVm
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
    public string? StudentId { get; set; }
    public decimal? GPA { get; set; }
    public string? CVFileUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateDate { get; set; }
    public List<string> Roles { get; set; } = new();
}

// ── Phân trang ────────────────────────────────────────────────
public class PaginationResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageIndex > 1;
    public bool HasNext => PageIndex < TotalPages;
}

// ── Filter + Pagination Request ───────────────────────────────
public class UserFilter
{
    public string? Keyword { get; set; }   // Tìm theo tên/email
    public string? Role { get; set; }   // Lọc theo role
    public bool? IsActive { get; set; }   // Lọc theo trạng thái
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ── Create Request ────────────────────────────────────────────
public class CreateUserRequest
{
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = "Intern";  // Admin/HR/Mentor/Intern

    public string? StudentId { get; set; }
    public decimal? GPA { get; set; }
}

// ── Update Request ────────────────────────────────────────────
public class UpdateUserRequest
{
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public string? Avatar { get; set; }
    public string? StudentId { get; set; }
    public decimal? GPA { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── Change Password Request ───────────────────────────────────
public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(6)] public string NewPassword { get; set; } = string.Empty;
}

// ── Assign Role Request ───────────────────────────────────────
public class AssignRoleRequest
{
    [Required] public List<string> Roles { get; set; } = new();
}