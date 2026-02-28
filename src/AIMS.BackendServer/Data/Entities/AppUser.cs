using Microsoft.AspNetCore.Identity;

namespace AIMS.BackendServer.Data.Entities;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    // Trường riêng cho Intern
    public string? StudentId { get; set; }
    public int? UniversityId { get; set; }
    public decimal? GPA { get; set; }
    public string? CVFileUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedDate { get; set; }

    // Navigation
    public University? University { get; set; }
}