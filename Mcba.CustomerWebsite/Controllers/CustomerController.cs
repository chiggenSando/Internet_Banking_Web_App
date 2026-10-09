using Mcba.CustomerWebsite.Filters;
using Mcba.CustomerWebsite.Utilities;
using Mcba.CustomerWebsite.ViewModels;
using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;


namespace Mcba.CustomerWebsite.Controllers;

[AuthorizeCustomer]
public class CustomerController : Controller
{
    //A1 Business rules 10 & 11
    private const decimal WithdrawFee = 0.05m;
    private const decimal TransferFee = 0.10m;
    private const int FreeTransactions = 2;

    private const string PendingTransactionKey = "PendingTransaction";

    private readonly McbaContext _context;

    private int CustomerID => HttpContext.Session.GetInt32(nameof(Customer.CustomerID)).Value;

    public CustomerController(McbaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Customer.Accounts is lazy loaded when the view uses it
        var customer = await _context.Customers.FindAsync(CustomerID);

        return View(customer);
    }

    // GET: /Customer/Transaction?type=W
    public async Task<IActionResult> Transaction(char type = TransactionTypes.Deposit)
    {
        ViewBag.Accounts = await AccountOptionsAsync();
        return View(new TransactionViewModel { TransactionType = type });
    }

    [HttpPost]
    public async Task<IActionResult> Transaction(TransactionViewModel model)
    {
        if (model.TransactionType is not (TransactionTypes.Deposit or TransactionTypes.Withdraw or TransactionTypes.Transfer))
            return BadRequest();

        // Only a transfer has a destination account.
        if (model.TransactionType != TransactionTypes.Transfer)
            model.DestinationAccountNumber = null;

        var account = await _context.Accounts.FindAsync(model.AccountNumber);
        if (account == null || account.CustomerID != CustomerID)
            ModelState.AddModelError(nameof(model.AccountNumber), "Please select one of your accounts.");

        // A1 business rule 14 (same check as W05's HasMoreThanTwoDecimalPlaces).
        if (decimal.Round(model.Amount, 2) != model.Amount)
            ModelState.AddModelError(nameof(model.Amount), "Amount cannot have more than 2 decimal places.");

        if (model.TransactionType == TransactionTypes.Transfer)
        {
            if (model.DestinationAccountNumber == null)
                ModelState.AddModelError(nameof(model.DestinationAccountNumber), "Please enter a destination account.");
            else if (model.DestinationAccountNumber == model.AccountNumber)
                ModelState.AddModelError(nameof(model.DestinationAccountNumber), "You cannot transfer to the same account.");
            else if (await _context.Accounts.FindAsync(model.DestinationAccountNumber) == null)
                ModelState.AddModelError(nameof(model.DestinationAccountNumber), "Destination account does not exist.");
        }

        // Withdraw and transfer: work out the fee, then check the minimum balance (A1 rules 5 and 6).
        if (ModelState.IsValid && model.TransactionType != TransactionTypes.Deposit)
        {
            model.ServiceCharge = await ServiceChargeAsync(account, model.TransactionType);
            var minimumBalance = account.AccountType == AccountTypes.Checking ? -100 : 0;
            if (account.Balance - model.Amount - model.ServiceCharge < minimumBalance)
                ModelState.AddModelError(nameof(model.Amount), "Insufficient funds for this amount and its service charge.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Accounts = await AccountOptionsAsync();
            return View(model);
        }

        HttpContext.Session.SetObject(PendingTransactionKey, model);
        return RedirectToAction(nameof(Confirm));
    }

    public IActionResult Confirm()
    {
        var model = HttpContext.Session.GetObject<TransactionViewModel>(PendingTransactionKey);
        if (model == null)
            return RedirectToAction(nameof(Index));

        return View(model);
    }

    [HttpPost, ActionName(nameof(Confirm))]
    public async Task<IActionResult> ConfirmPost()
    {
        var model = HttpContext.Session.GetObject<TransactionViewModel>(PendingTransactionKey);
        if (model == null)
            return RedirectToAction(nameof(Index));

        HttpContext.Session.Remove(PendingTransactionKey);

        var account = await _context.Accounts.FindAsync(model.AccountNumber);
        var timeUtc = DateTime.UtcNow;

        if (model.TransactionType == TransactionTypes.Deposit)
            account.Balance += model.Amount;
        else
            account.Balance -= model.Amount + model.ServiceCharge;

        // D, W, or the outgoing T (which records its destination).
        _context.Transactions.Add(new Transaction
        {
            TransactionType = model.TransactionType,
            AccountNumber = account.AccountNumber,
            DestinationAccountNumber = model.DestinationAccountNumber,
            Amount = model.Amount,
            Comment = model.Comment,
            TransactionTimeUtc = timeUtc
        });

        // Incoming T on the destination account.
        if (model.TransactionType == TransactionTypes.Transfer)
        {
            var destination = await _context.Accounts.FindAsync(model.DestinationAccountNumber);
            destination.Balance += model.Amount;
            _context.Transactions.Add(new Transaction
            {
                TransactionType = TransactionTypes.Transfer,
                AccountNumber = destination.AccountNumber,
                Amount = model.Amount,
                Comment = model.Comment,
                TransactionTimeUtc = timeUtc
            });
        }

        // A1 rule 15: the fee is its own S transaction.
        if (model.ServiceCharge > 0)
        {
            _context.Transactions.Add(new Transaction
            {
                TransactionType = TransactionTypes.ServiceCharge,
                AccountNumber = account.AccountNumber,
                Amount = model.ServiceCharge,
                Comment = $"{model.TypeName} service charge",
                TransactionTimeUtc = timeUtc
            });
        }

        // One SaveChanges = one database transaction: everything above is saved, or nothing is.
        await _context.SaveChangesAsync();

        TempData["Message"] = $"{model.TypeName} of {model.Amount:C} completed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Cancel()
    {
        HttpContext.Session.Remove(PendingTransactionKey);
        TempData["Message"] = "Transaction cancelled.";
        return RedirectToAction(nameof(Index));
    }

    // A1 rules 10-12: withdrawals and outgoing transfers use up the free transactions, then
    // cost $0.05 or $0.10. Deposits and incoming transfers never count and never cost.
    private async Task<decimal> ServiceChargeAsync(Account account, char type)
    {
        var used = await _context.Transactions.CountAsync(x => x.AccountNumber == account.AccountNumber &&
            (x.TransactionType == TransactionTypes.Withdraw ||
             x.TransactionType == TransactionTypes.Transfer && x.DestinationAccountNumber != null));

        if (used < FreeTransactions)
            return 0;

        return type == TransactionTypes.Withdraw ? WithdrawFee : TransferFee;
    }

    private async Task<List<SelectListItem>> AccountOptionsAsync()
    {
        var accounts = await _context.Accounts.Where(x => x.CustomerID == CustomerID).ToListAsync();
        return accounts.Select(x => new SelectListItem(
            $"{x.AccountNumber} ({(x.AccountType == AccountTypes.Savings ? "Savings" : "Checking")}) - {x.Balance:C}",
            x.AccountNumber.ToString())).ToList();
    }

}