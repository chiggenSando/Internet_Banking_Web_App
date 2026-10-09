using Mcba.AdminPortal.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminPortal.Controllers;

public class LoginController : Controller
{
    public const string SessionKey = "AdminUsername";

    // The spec allows fixed admin credentials that are not stored in the database.
    private const string AdminUsername = "admin";
    private const string AdminPassword = "admin";

    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (model.Username != AdminUsername || model.Password != AdminPassword)
        {
            ModelState.AddModelError(string.Empty, "Login failed, please try again.");
            return View(new AdminLoginViewModel { Username = model.Username });
        }

        HttpContext.Session.SetString(SessionKey, model.Username);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction(nameof(Login));
    }
}
