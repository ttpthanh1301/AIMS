using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AIMS.BackendServer.Services;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(AppUser user);
}

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwt;
    private readonly UserManager<AppUser> _userManager;

    public TokenService(
        JwtSettings jwt,
        UserManager<AppUser> userManager)
    {
        _jwt = jwt;
        _userManager = userManager;
    }

    public async Task<string> GenerateAccessTokenAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            // ⭐ QUAN TRỌNG — Identity đọc từ đây
            new Claim(ClaimTypes.NameIdentifier, user.Id),

            // Có thể giữ sub để tương thích chuẩn JWT
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),

            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new Claim("firstName", user.FirstName ?? ""),
            new Claim("lastName", user.LastName ?? "")
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}