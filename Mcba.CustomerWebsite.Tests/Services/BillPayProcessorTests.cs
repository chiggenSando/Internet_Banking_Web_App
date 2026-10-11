using Mcba.CustomerWebsite.Services;
using Mcba.Data;
using Mcba.Data.Models;

namespace Mcba.CustomerWebsite.Tests.Services;

public class BillPayProcessorTests : IDisposable
{
    // A fixed "now" so tests never depend on the real clock.
    private static readonly DateTime Now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);

    private readonly McbaContext _context = TestDatabase.Create();
    private readonly BillPayProcessor _processor;

    public BillPayProcessorTests() => _processor = new BillPayProcessor(_context);

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task ProcessDue_OneOffBill_DebitsAccountRecordsTransactionAndRemovesBill()
    {
        TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 100, Now.AddMinutes(-1));

        await _processor.ProcessDueAsync(Now);

        _context.ChangeTracker.Clear();
        Assert.Equal(400, Balance(TestDatabase.SavingsAccount));
        Assert.Empty(_context.BillPays);

        // Exactly one transaction: BillPay never charges a service fee.
        var transaction = Assert.Single(_context.Transactions);
        Assert.Equal(TransactionTypes.BillPay, transaction.TransactionType);
        Assert.Equal(TestDatabase.SavingsAccount, transaction.AccountNumber);
        Assert.Equal(100, transaction.Amount);
        Assert.Equal(Now, transaction.TransactionTimeUtc);
        Assert.Null(transaction.DestinationAccountNumber);
    }

    [Fact]
    public async Task ProcessDue_AnnualBill_DebitsAccountAndReschedulesOneYearLater()
    {
        var scheduled = Now.AddMinutes(-1);
        TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 100, scheduled, BillPayPeriods.Annual);

        await _processor.ProcessDueAsync(Now);

        _context.ChangeTracker.Clear();
        Assert.Equal(400, Balance(TestDatabase.SavingsAccount));
        var bill = Assert.Single(_context.BillPays);
        Assert.Equal(scheduled.AddYears(1), bill.ScheduleTimeUtc);
        Assert.False(bill.IsFailed);
        Assert.Single(_context.Transactions);
    }

    // Savings cannot go below $0 and checking cannot go below -$100; both start at $500.
    [Theory]
    [InlineData(TestDatabase.SavingsAccount, 499.99, false)]  // ends at $0.01
    [InlineData(TestDatabase.SavingsAccount, 500, false)]     // ends at exactly $0
    [InlineData(TestDatabase.SavingsAccount, 500.01, true)]   // would end at -$0.01
    [InlineData(TestDatabase.CheckingAccount, 599.99, false)] // ends at -$99.99
    [InlineData(TestDatabase.CheckingAccount, 600, false)]    // ends at exactly -$100
    [InlineData(TestDatabase.CheckingAccount, 600.01, true)]  // would end at -$100.01
    public async Task ProcessDue_RespectsMinimumBalance(int accountNumber, double amount, bool shouldFail)
    {
        TestDatabase.AddBill(_context, accountNumber, (decimal)amount, Now.AddMinutes(-1));

        await _processor.ProcessDueAsync(Now);

        _context.ChangeTracker.Clear();
        if (shouldFail)
        {
            Assert.Equal(500, Balance(accountNumber));
            Assert.True(Assert.Single(_context.BillPays).IsFailed);
            Assert.Empty(_context.Transactions);
        }
        else
        {
            Assert.Equal(500 - (decimal)amount, Balance(accountNumber));
            Assert.Empty(_context.BillPays);
            Assert.Single(_context.Transactions);
        }
    }

    [Theory]
    [InlineData(-1, true)] // a second overdue
    [InlineData(0, true)]  // due exactly now
    [InlineData(1, false)] // a second in the future
    public async Task ProcessDue_OnlyProcessesBillsThatAreDue(int secondsFromNow, bool shouldProcess)
    {
        TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 100, Now.AddSeconds(secondsFromNow));

        await _processor.ProcessDueAsync(Now);

        _context.ChangeTracker.Clear();
        Assert.Equal(shouldProcess ? 400 : 500, Balance(TestDatabase.SavingsAccount));
        Assert.Equal(shouldProcess ? 0 : 1, _context.BillPays.Count());
    }

    [Fact]
    public async Task ProcessDue_FailedBill_IsSkippedUntilRetried()
    {
        TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 100, Now.AddMinutes(-1), isFailed: true);

        await _processor.ProcessDueAsync(Now);

        _context.ChangeTracker.Clear();
        Assert.Equal(500, Balance(TestDatabase.SavingsAccount));
        Assert.True(Assert.Single(_context.BillPays).IsFailed);
        Assert.Empty(_context.Transactions);
    }

    [Fact]
    public async Task ProcessDue_MultipleBills_PaysInScheduleOrderUntilFundsRunOut()
    {
        // Added out of order: the earlier bill must be paid first, leaving too little for the later one.
        var laterId = TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 300, Now.AddMinutes(-1));
        TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 300, Now.AddMinutes(-2));

        await _processor.ProcessDueAsync(Now);

        _context.ChangeTracker.Clear();
        Assert.Equal(200, Balance(TestDatabase.SavingsAccount));
        var remaining = Assert.Single(_context.BillPays);
        Assert.Equal(laterId, remaining.BillPayID);
        Assert.True(remaining.IsFailed);
        Assert.Single(_context.Transactions);
    }

    [Fact]
    public async Task ProcessDue_FailedBillOnOneAccount_DoesNotBlockOtherAccounts()
    {
        TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 1000, Now.AddMinutes(-2));
        TestDatabase.AddBill(_context, TestDatabase.CheckingAccount, 100, Now.AddMinutes(-1));

        await _processor.ProcessDueAsync(Now);

        _context.ChangeTracker.Clear();
        Assert.Equal(500, Balance(TestDatabase.SavingsAccount));
        Assert.Equal(400, Balance(TestDatabase.CheckingAccount));
        Assert.True(Assert.Single(_context.BillPays).IsFailed);
    }

    [Fact]
    public async Task ProcessDue_RunTwice_DoesNotPayTwice()
    {
        TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 100, Now.AddMinutes(-1), BillPayPeriods.Annual);

        await _processor.ProcessDueAsync(Now);
        await _processor.ProcessDueAsync(Now.AddSeconds(10));

        _context.ChangeTracker.Clear();
        Assert.Equal(400, Balance(TestDatabase.SavingsAccount));
        Assert.Single(_context.Transactions);
    }

    private decimal Balance(int accountNumber) => _context.Accounts.Single(x => x.AccountNumber == accountNumber).Balance;
}
