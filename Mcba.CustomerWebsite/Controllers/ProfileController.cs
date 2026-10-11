using Mcba.CustomerWebsite.Filters;
using Mcba.CustomerWebsite.ViewModels;
using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleHashing.Net;

namespace Mcba.CustomerWebsite.Controllers;

[AuthorizeCustomer]
public class ProfileController : Controller
{
    private static readonly ISimpleHash s_simpleHash = new SimpleHash();

    private readonly McbaContext _context;

    private int CustomerID => HttpContext.Session.GetInt32(nameof(Customer.CustomerID)).Value;

    public ProfileController(McbaContext context)
    {
        _context = context;
    }

    // Read-only view of the customers details
    public async Task<IActionResult> Index() => View(await _context.Customers.FindAsync(CustomerID));

    public async Task<IActionResult> Edit()
    {
        var customer = await _context.Customers.FindAsync(CustomerID);
        if (customer == null)
            return NotFound();

        return View(new ProfileViewModel
        {
            Name = customer.Name,
            TFN = customer.TFN,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Postcode = customer.Postcode,
            Mobile = customer.Mobile
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var customer = await _context.Customers.FindAsync(CustomerID);
        customer.Name = model.Name;
        customer.TFN = model.TFN;
        customer.Address = model.Address;
        customer.City = model.City;
        customer.State = model.State;
        customer.Postcode = model.Postcode;
        customer.Mobile = model.Mobile;
        await _context.SaveChangesAsync();

        // Keep the "Welcome, <name>" in the nav bar up to date.
        HttpContext.Session.SetString(nameof(Customer.Name), customer.Name);

        TempData["Message"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult ChangePassword() => View();

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var login = await _context.Logins.FirstAsync(x => x.CustomerID == CustomerID);

        if (ModelState.IsValid && !s_simpleHash.Verify(model.CurrentPassword, login.PasswordHash))
            ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");

        if (!ModelState.IsValid)
            return View(model);

        // Store only the hash
        login.PasswordHash = s_simpleHash.Compute(model.NewPassword);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Your password has been changed.";
        return RedirectToAction(nameof(Index));
    }

}