using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mcba.CustomerWebsite.Services;

// Executes scheduled bill payments that are due. Kept separate from the
// background service so it can be unit tested with any "now".
public class BillPayProcessor(McbaContext context)
{
    private readonly McbaContext _context = context;

    public async Task ProcessDueAsync(DateTime nowUtc)
    {
        var dueBills = await _context.BillPays
            .Include(x => x.Account)
            .Where(x => !x.IsFailed && x.ScheduleTimeUtc <= nowUtc)
            .OrderBy(x => x.ScheduleTimeUtc)
            .ToListAsync();

        foreach (var bill in dueBills)
        {
            // Insufficient funds: keep the bill but flag it so the customer can retry or cancel.
            if (bill.Account.Balance - bill.Amount < bill.Account.MinimumBalance)
            {
                bill.IsFailed = true;
                await _context.SaveChangesAsync();
                continue;
            }

            // BillPay never incurs a service charge or uses a free transaction.
            bill.Account.Balance -= bill.Amount;
            _context.Transactions.Add(new Transaction
            {
                TransactionType = TransactionTypes.BillPay,
                AccountNumber = bill.AccountNumber,
                Amount = bill.Amount,
                Comment = "BillPay",
                TransactionTimeUtc = nowUtc
            });

            if (bill.Period == BillPayPeriods.Annual)
                bill.ScheduleTimeUtc = bill.ScheduleTimeUtc.AddYears(1);
            else
                _context.BillPays.Remove(bill);

            // One SaveChanges per bill: the debit, transaction and reschedule succeed or fail together.
            await _context.SaveChangesAsync();
        }
    }
}
