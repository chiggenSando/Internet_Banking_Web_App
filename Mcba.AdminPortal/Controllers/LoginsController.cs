using Mcba.AdminPortal.Filters;
using Mcba.AdminPortal.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminPortal.Controllers;

[AuthorizeAdmin]
public class LoginsController(IHttpClientFactory clientFactory) : Controller
{
    private HttpClient Client => clientFactory.CreateClient("AdminApi");

    // GET: Logins
    public async Task<IActionResult> Index()
    {
        var logins = await Client.GetFromJsonAsync<List<LoginViewModel>>("api/logins");

        return View(logins);
    }

    // POST: Logins/Lock/12345678
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(string id) => await SetLocked(id, true);

    // POST: Logins/Unlock/12345678
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id) => await SetLocked(id, false);

    private async Task<IActionResult> SetLocked(string id, bool locked)
    {
        var action = locked ? "lock" : "unlock";
        var response = await Client.PutAsync($"api/logins/{Uri.EscapeDataString(id)}/{action}", null);

        TempData["Message"] = response.IsSuccessStatusCode
            ? $"Login {id} was {action}ed."
            : $"Login {id} could not be {action}ed.";

        return RedirectToAction(nameof(Index));
    }
}
