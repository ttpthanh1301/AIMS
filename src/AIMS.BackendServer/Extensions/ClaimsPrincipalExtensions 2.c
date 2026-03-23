using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace AIMS.BackendServer.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        // Dùng đúng URL string từ token đã decode
        return user.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? "";
    }
}