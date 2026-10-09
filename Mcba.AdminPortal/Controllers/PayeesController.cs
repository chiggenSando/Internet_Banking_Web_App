using Mcba.AdminPortal.Filters;
using Mcba.AdminPortal.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminPortal.Controllers;

[AuthorizeAdmin]
public class PayeesController(IHttpClientFactory clientFactory) : Controller
{
    private HttpClient Client => clientFactory.CreateClient("AdminApi");

    // GET: Payees?state=VIC
    public async Task<IActionResult> Index(string state)
    {
        var url = string.IsNullOrWhiteSpace(state) ? "api/payees" : $"api/payees?state={Uri.EscapeDataString(state)}";
        var payees = await Client.GetFromJsonAsync<List<PayeeViewModel>>(url);

        ViewData["State"] = state;
        return View(payees);
    }

    // GET: Payees/Create
    public IActionResult Create() => View();

    // POST: Payees/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PayeeViewModel payee)
    {
        if (!ModelState.IsValid)
            return View(payee);

        var response = await Client.PostAsJsonAsync("api/payees", payee);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "The payee could not be created, please check the details and try again.");
            return View(payee);
        }

        TempData["Message"] = $"Payee {payee.Name} was created.";
        return RedirectToAction(nameof(Index));
    }
}
