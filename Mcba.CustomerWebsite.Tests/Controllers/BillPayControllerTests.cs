using System.ComponentModel.DataAnnotations;
using Mcba.CustomerWebsite.Controllers;
using Mcba.CustomerWebsite.ViewModels;
using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mcba.CustomerWebsite.Tests.Controllers;

public class BillPayControllerTests : IDisposable
{
    private readonly McbaContext _context = TestDatabase.Create();
    private readonly BillPayController _controller;

    public BillPayControllerTests() =>
        _controller = new BillPayController(_context).LoggedInAs(TestDatabase.CustomerID);

    public void Dispose() => _context.Dispose();

    // --- Index ---

    [Fact]
    public async Task Index_ShowsOnlyTheCustomersBillsInScheduleOrder()
    {
        var now = DateTime.UtcNow;
        var later = TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 10, now.AddDays(2));
        var sooner = TestDatabase.AddBill(_context, TestDatabase.CheckingAccount, 10, now.AddDays(1));
        TestDatabase.AddBill(_context, TestDatabase.OtherCustomerAccount, 10, now.AddDays(1));

        var result = await _controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var bills = Assert.IsAssignableFrom<IEnumerable<BillPay>>(view.Model);
        Assert.Equal([sooner, later], bills.Select(x => x.BillPayID));
    }

    // --- Create ---

    [Fact]
    public async Task CreateGet_DefaultsToNextWholeHourAndListsOnlyOwnAccounts()
    {
        var result = await _controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<BillPayViewModel>(view.Model);
        Assert.True(model.ScheduleTime > DateTime.Now);
        Assert.Equal(0, model.ScheduleTime.Minute);
        Assert.Equal(BillPayPeriods.OneOff, model.Period);
        AssertOnlyOwnAccountsListed();
    }

    [Theory]
    [InlineData(BillPayPeriods.OneOff)]
    [InlineData(BillPayPeriods.Annual)]
    public async Task CreatePost_Valid_SavesBillInUtcAndRedirects(char period)
    {
        var model = ValidModel();
        model.Period = period;

        var result = await _controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(BillPayController.Index), redirect.ActionName);

        var bill = Assert.Single(_context.BillPays);
        Assert.Equal(TestDatabase.SavingsAccount, bill.AccountNumber);
        Assert.Equal(TestDatabase.PayeeID(_context), bill.PayeeID);
        Assert.Equal(model.Amount, bill.Amount);
        Assert.Equal(period, bill.Period);
        Assert.Equal(model.ScheduleTime.ToUniversalTime(), bill.ScheduleTimeUtc);
        Assert.False(bill.IsFailed);
    }

    [Theory]
    [InlineData(TestDatabase.OtherCustomerAccount)] // someone else's account
    [InlineData(TestDatabase.MissingAccount)]
    public async Task CreatePost_AccountNotOwned_ReturnsViewWithError(int accountNumber)
    {
        var model = ValidModel();
        model.AccountNumber = accountNumber;

        await AssertRejectedAsync(model, nameof(BillPayViewModel.AccountNumber));
    }

    [Fact]
    public async Task CreatePost_MissingPayee_ReturnsViewWithError()
    {
        var model = ValidModel();
        model.PayeeID = 999;

        await AssertRejectedAsync(model, nameof(BillPayViewModel.PayeeID));
    }

    [Theory]
    [InlineData(1.1, true)]
    [InlineData(1.11, true)]
    [InlineData(1.111, false)]
    public async Task CreatePost_AmountAllowsAtMostTwoDecimalPlaces(double amount, bool valid)
    {
        var model = ValidModel();
        model.Amount = (decimal)amount;

        await AssertAcceptedOrRejectedAsync(valid, model, nameof(BillPayViewModel.Amount));
    }

    [Theory]
    [InlineData(-60, false)] // a minute ago
    [InlineData(60, true)]   // a minute from now
    public async Task CreatePost_ScheduleTimeMustBeInTheFuture(int secondsFromNow, bool valid)
    {
        var model = ValidModel();
        model.ScheduleTime = DateTime.Now.AddSeconds(secondsFromNow);

        await AssertAcceptedOrRejectedAsync(valid, model, nameof(BillPayViewModel.ScheduleTime));
    }

    [Theory]
    [InlineData('X')]
    [InlineData('M')] // monthly is not supported
    [InlineData('\0')]
    public async Task CreatePost_InvalidPeriod_ReturnsViewWithError(char period)
    {
        var model = ValidModel();
        model.Period = period;

        await AssertRejectedAsync(model, nameof(BillPayViewModel.Period));
    }

    // [Range] is checked by model binding, which a direct controller call skips, so it is tested with the same validator.
    [Theory]
    [InlineData(0.01, true)]
    [InlineData(0, false)]
    [InlineData(-0.01, false)]
    public void Amount_MustBePositive(double amount, bool valid)
    {
        var model = ValidModel();
        model.Amount = (decimal)amount;

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        Assert.Equal(!valid, results.Any(r => r.MemberNames.Contains(nameof(BillPayViewModel.Amount))));
    }

    // --- Retry ---

    [Fact]
    public async Task Retry_OwnFailedBill_ClearsFailureAndRedirects()
    {
        var id = TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 10, DateTime.UtcNow, isFailed: true);

        var result = await _controller.Retry(id);

        Assert.IsType<RedirectToActionResult>(result);
        _context.ChangeTracker.Clear();
        Assert.False(_context.BillPays.Single().IsFailed);
    }

    [Fact]
    public async Task Retry_OtherCustomersBill_ReturnsNotFoundAndLeavesItFailed()
    {
        var id = TestDatabase.AddBill(_context, TestDatabase.OtherCustomerAccount, 10, DateTime.UtcNow, isFailed: true);

        var result = await _controller.Retry(id);

        Assert.IsType<NotFoundResult>(result);
        _context.ChangeTracker.Clear();
        Assert.True(_context.BillPays.Single().IsFailed);
    }

    [Fact]
    public async Task Retry_MissingBill_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Retry(999));
    }

    // --- Cancel ---

    [Fact]
    public async Task Cancel_OwnBill_RemovesItAndRedirects()
    {
        var id = TestDatabase.AddBill(_context, TestDatabase.SavingsAccount, 10, DateTime.UtcNow.AddDays(1));

        var result = await _controller.Cancel(id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(_context.BillPays);
    }

    [Fact]
    public async Task Cancel_OtherCustomersBill_ReturnsNotFoundAndKeepsIt()
    {
        var id = TestDatabase.AddBill(_context, TestDatabase.OtherCustomerAccount, 10, DateTime.UtcNow.AddDays(1));

        var result = await _controller.Cancel(id);

        Assert.IsType<NotFoundResult>(result);
        Assert.Single(_context.BillPays);
    }

    [Fact]
    public async Task Cancel_MissingBill_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _controller.Cancel(999));
    }

    // --- Helpers ---

    private BillPayViewModel ValidModel() => new()
    {
        AccountNumber = TestDatabase.SavingsAccount,
        PayeeID = TestDatabase.PayeeID(_context),
        Amount = 50,
        ScheduleTime = DateTime.Now.AddDays(1),
        Period = BillPayPeriods.OneOff
    };

    private async Task AssertAcceptedOrRejectedAsync(bool valid, BillPayViewModel model, string field)
    {
        if (valid)
        {
            Assert.IsType<RedirectToActionResult>(await _controller.Create(model));
            Assert.Single(_context.BillPays);
        }
        else
        {
            await AssertRejectedAsync(model, field);
        }
    }

    // An invalid form redisplays the view with an error on the field, saves nothing, and refills the dropdowns.
    private async Task AssertRejectedAsync(BillPayViewModel model, string field)
    {
        var result = await _controller.Create(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.True(_controller.ModelState.ContainsKey(field));
        Assert.Single(_controller.ModelState, x => x.Value!.Errors.Count > 0);
        Assert.Empty(_context.BillPays);
        AssertOnlyOwnAccountsListed();
    }

    private void AssertOnlyOwnAccountsListed()
    {
        List<SelectListItem> accounts = Assert.IsType<List<SelectListItem>>(_controller.ViewBag.Accounts);
        Assert.Equal(
            [TestDatabase.SavingsAccount.ToString(), TestDatabase.CheckingAccount.ToString()],
            accounts.Select(x => x.Value).Order());
    }
}
