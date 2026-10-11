using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mcba.CustomerWebsite.Tests;

// Builds a fresh, seeded in-memory McbaContext. Each call gets a unique database name so tests never share state.
public static class TestDatabase
{
    public const int CustomerID = 2100;
    public const int SavingsAccount = 4100;
    public const int CheckingAccount = 4101;

    public const int OtherCustomerID = 2200;
    public const int OtherCustomerAccount = 4200;

    public const int MissingAccount = 9999;

    public static McbaContext Create()
    {
        var options = new DbContextOptionsBuilder<McbaContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new McbaContext(options);

        context.Customers.AddRange(
            new Customer { CustomerID = CustomerID, Name = "Matthew Bolger" },
            new Customer { CustomerID = OtherCustomerID, Name = "Rodney Cocker" });
        context.Accounts.AddRange(
            new Account { AccountNumber = SavingsAccount, AccountType = AccountTypes.Savings, CustomerID = CustomerID, Balance = 500 },
            new Account { AccountNumber = CheckingAccount, AccountType = AccountTypes.Checking, CustomerID = CustomerID, Balance = 500 },
            new Account { AccountNumber = OtherCustomerAccount, AccountType = AccountTypes.Savings, CustomerID = OtherCustomerID, Balance = 500 });
        context.Payees.Add(new Payee
        {
            Name = "Telstra",
            Address = "123 Collins Street",
            City = "Melbourne",
            State = "VIC",
            Postcode = "3000",
            Phone = "(03) 9999 8888"
        });
        context.SaveChanges();

        context.ChangeTracker.Clear();
        return context;
    }

    public static int PayeeID(McbaContext context) => context.Payees.Single().PayeeID;

    // Adds a bill and returns its generated ID.
    public static int AddBill(McbaContext context, int accountNumber, decimal amount, DateTime scheduleTimeUtc,
        char period = BillPayPeriods.OneOff, bool isFailed = false)
    {
        var bill = new BillPay
        {
            AccountNumber = accountNumber,
            PayeeID = PayeeID(context),
            Amount = amount,
            ScheduleTimeUtc = scheduleTimeUtc,
            Period = period,
            IsFailed = isFailed
        };
        context.BillPays.Add(bill);
        context.SaveChanges();
        context.ChangeTracker.Clear();
        return bill.BillPayID;
    }
}
