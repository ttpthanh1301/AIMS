using System.Net.Http.Json;
using System.Security.Claims;
using AIMS.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpFactory;

    public AccountController(IHttpClientFactory httpFactory)
        => _httpFactory = httpFactory;

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid) return View(request);

        try
        {
            var http = _httpFactory.CreateClient("AimsApi");
            var resp = await http.PostAsJsonAsync("/api/auth/login", request);

            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = "Email hoặc mật khẩu không đúng.";
                return View(request);
            }

            var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth == null) return View(request);

            // Tạo Claims Identity lưu vào Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, auth.UserId),
                new Claim(ClaimTypes.Email,          auth.Email),
                new Claim(ClaimTypes.Name,           auth.FullName),
                new Claim("AccessToken",             auth.AccessToken),
            };

            foreach (var role in auth.Roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            // Redirect theo Role
            return auth.Roles.FirstOrDefault() switch
            {
                "Admin"  => RedirectToAction("Index", "Home", new { area = "Admin" }),
                "HR"     => RedirectToAction("Index", "Home", new { area = "HR" }),
                "Mentor" => RedirectToAction("Index", "Home", new { area = "Mentor" }),
                _        => RedirectToAction("Index", "Home"),
            };
        }
        catch
        {
            TempData["Error"] = "Không thể kết nối tới server. Vui lòng thử lại.";
            return View(request);
        }
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}