using Microsoft.AspNetCore.Identity;

namespace AIMS.BackendServer.Data.Entities;

public class AppRole : IdentityRole
{
    public string? Description { get; set; }
}