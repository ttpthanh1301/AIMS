using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.Intern.Controllers;

[Area("Intern")]
[Authorize(Roles = "Intern")]
public class ApplyController : Controller
{
    public IActionResult Index()
        => NotFound();

    [HttpPost]
    public IActionResult Apply()
        => NotFound();
}
