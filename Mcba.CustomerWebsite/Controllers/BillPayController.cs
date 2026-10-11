using Mcba.CustomerWebsite.Filters;
using Mcba.CustomerWebsite.ViewModels;
using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Mcba.CustomerWebsite.Controllers;

[AuthorizeCustomer]
public class BillPayController(McbaContext context) : Controller
{
    private readonly McbaContext _context = context;

    private int CustomerID => HttpContext.Session.GetInt32(nameof(Customer.CustomerID)).Value;

    // GET: /BillPay
    public async Task<IActionResult> Index()
    {
        var bills = await _context.BillPays
            .Include(x => x.Payee)
            .Where(x => x.Account.CustomerID == CustomerID)
            .OrderBy(x => x.ScheduleTimeUtc)
            .ToListAsync();

        return View(bills);
    }

    // GET: /BillPay/Create
    public async Task<IActionResult> Create()
    {
        await SetSelectListsAsync();

        // Default to the next whole hour.
        var now = DateTime.Now;
        var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
        return View(new BillPayViewModel { ScheduleTime = nextHour });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillPayViewModel model)
    {
        var account = await _context.Accounts.FindAsync(model.AccountNumber);
        if (account == null || account.CustomerID != CustomerID)
            ModelState.AddModelError(nameof(model.AccountNumber), "Please select one of your accounts.");

        if (await _context.Payees.FindAsync(model.PayeeID) == null)
            ModelState.AddModelError(nameof(model.PayeeID), "Please select a payee.");

        if (decimal.Round(model.Amount, 2) != model.Amount)
            ModelState.AddModelError(nameof(model.Amount), "Amount cannot have more than 2 decimal places.");

        if (model.ScheduleTime <= DateTime.Now)
            ModelState.AddModelError(nameof(model.ScheduleTime), "Scheduled time must be in the future.");

        if (model.Period is not (BillPayPeriods.OneOff or BillPayPeriods.Annual))
            ModelState.AddModelError(nameof(model.Period), "Please select how often to pay.");

        if (!ModelState.IsValid)
        {
            await SetSelectListsAsync();
            return View(model);
        }

        _context.BillPays.Add(new BillPay
        {
            AccountNumber = model.AccountNumber,
            PayeeID = model.PayeeID,
            Amount = model.Amount,
            ScheduleTimeUtc = DateTime.SpecifyKind(model.ScheduleTime, DateTimeKind.Local).ToUniversalTime(),
            Period = model.Period
        });
        await _context.SaveChangesAsync();

        TempData["Message"] = $"Bill payment of {model.Amount:C} scheduled.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /BillPay/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var bill = await FindOwnBillAsync(id);
        if (bill == null)
            return NotFound();

        _context.BillPays.Remove(bill);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Scheduled bill payment cancelled.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /BillPay/Retry/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(int id)
    {
        var bill = await FindOwnBillAsync(id);
        if (bill == null)
            return NotFound();

        // The background service picks the bill up again on its next run.
        bill.IsFailed = false;
        await _context.SaveChangesAsync();

        TempData["Message"] = "Bill payment will be retried shortly.";
        return RedirectToAction(nameof(Index));
    }

    // Only returns the bill if it belongs to one of the logged in customer's accounts.
    private async Task<BillPay> FindOwnBillAsync(int id)
    {
        return await _context.BillPays
            .FirstOrDefaultAsync(x => x.BillPayID == id && x.Account.CustomerID == CustomerID);
    }

    private async Task SetSelectListsAsync()
    {
        var accounts = await _context.Accounts.Where(x => x.CustomerID == CustomerID).ToListAsync();
        ViewBag.Accounts = accounts.Select(x => new SelectListItem(
            $"{x.AccountNumber} ({(x.AccountType == AccountTypes.Savings ? "Savings" : "Checking")}) - {x.Balance:C}",
            x.AccountNumber.ToString())).ToList();

        var payees = await _context.Payees.OrderBy(x => x.Name).ToListAsync();
        ViewBag.Payees = payees.Select(x => new SelectListItem(
            $"{x.Name} ({x.City}, {x.State})", x.PayeeID.ToString())).ToList();
    }
}
