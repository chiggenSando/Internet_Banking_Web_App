using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mcba.AdminPortal.Filters;
using Mcba.AdminPortal.Models;

namespace Mcba.AdminPortal.Controllers;

public class HomeController : Controller
{
    [AuthorizeAdmin]
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
