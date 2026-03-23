using System.Net.Http.Json;
using System.Security.Claims;
using AIMS.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _factory;

    public AccountController(IHttpClientFactory factory)
        => _factory = factory;

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var client = _factory.CreateClient("AimsApi");
            var resp = await client.PostAsJsonAsync("/api/auth/login", request);

            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = "Email hoặc mật khẩu không đúng.";
                return View(request);
            }

            var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth == null) return View(request);

            // Tạo Claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, auth.UserId),
                new(ClaimTypes.Email,          auth.Email),
                new(ClaimTypes.Name,           auth.FullName),
                new("AccessToken",             auth.AccessToken),
            };
            foreach (var role in auth.Roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            // Redirect theo Role
            return auth.Roles.FirstOrDefault() switch
            {
                "HR" => RedirectToAction("Index", "JobDescription", new { area = "HR" }),
                "Mentor" => RedirectToAction("Index", "Dashboard", new { area = "Mentor" }),
                "Intern" => RedirectToAction("Index", "LMS", new { area = "Intern" }),
                _ => RedirectToAction("Index", "Home"),
            };
        }
        catch
        {
            TempData["Error"] = "Không thể kết nối server.";
            return View(request);
        }
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}