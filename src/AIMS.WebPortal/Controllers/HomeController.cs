using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIMS.WebPortal.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (roles.Contains("Admin"))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        if (roles.Contains("HR"))
            return RedirectToAction("Index", "JobDescription", new { area = "HR" });

        if (roles.Contains("Mentor"))
            return RedirectToAction("Index", "Dashboard", new { area = "Mentor" });

        if (roles.Contains("Intern"))
            return RedirectToAction("Index", "Task", new { area = "Intern" });

        ViewData["Title"] = "Dashboard";
        return View();
    }
}
