using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Mvc;
using SimpleHashing.Net;

namespace Mcba.CustomerWebsite.Controllers;

public class LoginController : Controller
{   
    private static readonly ISimpleHash s_simpleHash = new SimpleHash();

    private readonly McbaContext _context;

    public LoginController(McbaContext context)
    {
        _context = context;
    }

    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string loginID, string password)
    {
        var login = await _context.Logins.FindAsync(loginID);
        if (login == null || string.IsNullOrEmpty(password) || !s_simpleHash.Verify(password, login.PasswordHash))
        {
            ModelState.AddModelError("LoginFailed", "Login failed please try again.");
            return View(new Login { LoginID = loginID});
        }

        if (login.IsLocked)
        {
            ModelState.AddModelError("LoginFailed", "Your account is locked, please contact MCBA.");
            return View(new Login { LoginID = loginID});
        }

        //Login Customer
        HttpContext.Session.SetInt32(nameof(Customer.CustomerID), login.CustomerID);
        HttpContext.Session.SetString(nameof(Customer.Name), login.Customer.Name);

        return RedirectToAction("Index", "Customer");
    }

    public IActionResult Logout()
    {
        //Logout Customer.
        HttpContext.Session.Clear();

        return RedirectToAction(nameof(Login));
    }

}

