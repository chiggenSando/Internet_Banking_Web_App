using Mcba.CustomerWebsite.Controllers;
using Mcba.CustomerWebsite.Tests.Common;
using Mcba.CustomerWebsite.Tests.Extensions;
using Mcba.CustomerWebsite.Utilities;
using Mcba.CustomerWebsite.ViewModels;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;
using Xunit;

namespace Mcba.CustomerWebsite.Tests;

public class CustomerControllerTests : TestBase
{
    private const string PendingTransactionKey = "PendingTransaction";

    private readonly CustomerController _controller;

    public CustomerControllerTests()
    {
        _controller = WithLoggedInCustomer(new CustomerController(Context));
    }

    // Runs validation like MVC does, then calls the POST action.
    private Task<IActionResult> PostTransactionAsync(TransactionViewModel model)
    {
        _controller.ModelState.ValidateObject(model);
        return _controller.Transaction(model);
    }

    private TransactionViewModel PendingTransaction =>
        _controller.HttpContext.Session.GetObject<TransactionViewModel>(PendingTransactionKey);

    [Fact]
    public async Task Index_ReturnsViewWithLoggedInCustomer()
    {
        // Act.
        var result = await _controller.Index();

        // Assert.
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(CustomerID, Assert.IsType<Customer>(view.Model).CustomerID);
    }

    [Theory]
    [InlineData(TransactionTypes.Deposit)]
    [InlineData(TransactionTypes.Withdraw)]
    [InlineData(TransactionTypes.Transfer)]
    public async Task Transaction_Get_ReturnsFormForTypeWithOnlyOwnAccounts(char type)
    {
        // Act.
        var result = await _controller.Transaction(type);

        // Assert.
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(type, Assert.IsType<TransactionViewModel>(view.Model).TransactionType);
        Assert.Equal(2, ((List<SelectListItem>)view.ViewData["Accounts"]).Count);
    }

    [Theory]
    [InlineData(TransactionTypes.Deposit, null)]
    [InlineData(TransactionTypes.Withdraw, null)]
    [InlineData(TransactionTypes.Transfer, 4100)]
    [InlineData(TransactionTypes.Transfer, 4200)]
    public async Task Transaction_Post_Valid_StoresPendingAndRedirectsToConfirm(char type, int? destination)
    {
        // Arrange.
        var model = new TransactionViewModel
        {
            TransactionType = type, AccountNumber = 4101, DestinationAccountNumber = destination, Amount = 10, Comment = "Test"
        };

        // Act.
        var result = await PostTransactionAsync(model);

        // Assert.
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CustomerController.Confirm), redirect.ActionName);
        Assert.Equal(type, PendingTransaction.TransactionType);
        Assert.Equal(10, PendingTransaction.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1.234)]
    public async Task Transaction_Post_InvalidAmount_ReturnsViewWithError(double amount)
    {
        // Arrange.
        var model = new TransactionViewModel
        {
            TransactionType = TransactionTypes.Deposit, AccountNumber = 4100, Amount = (decimal)amount
        };

        // Act.
        var result = await PostTransactionAsync(model);

        // Assert.
        Assert.IsType<ViewResult>(result);
        Assert.True(_controller.ModelState.ContainsKey(nameof(TransactionViewModel.Amount)));
        Assert.Null(PendingTransaction);
    }

    [Theory]
    [InlineData(null)]  // no destination
    [InlineData(4101)]  // same as source
    [InlineData(9999)]  // does not exist
    public async Task Transaction_Post_InvalidTransferDestination_ReturnsViewWithError(int? destination)
    {
        // Arrange.
        var model = new TransactionViewModel
        {
            TransactionType = TransactionTypes.Transfer, AccountNumber = 4101, DestinationAccountNumber = destination, Amount = 10
        };

        // Act.
        var result = await PostTransactionAsync(model);

        // Assert.
        Assert.IsType<ViewResult>(result);
        Assert.True(_controller.ModelState.ContainsKey(nameof(TransactionViewModel.DestinationAccountNumber)));
    }

    [Fact]
    public async Task Transaction_Post_OtherCustomersAccount_ReturnsViewWithError()
    {
        // Arrange: account 4200 belongs to customer 2200.
        var model = new TransactionViewModel { TransactionType = TransactionTypes.Withdraw, AccountNumber = 4200, Amount = 10 };

        // Act.
        var result = await PostTransactionAsync(model);

        // Assert.
        Assert.IsType<ViewResult>(result);
        Assert.True(_controller.ModelState.ContainsKey(nameof(TransactionViewModel.AccountNumber)));
    }

    [Theory]
    [InlineData(TransactionTypes.ServiceCharge)]
    [InlineData(TransactionTypes.BillPay)]
    public async Task Transaction_Post_TypeNotAllowed_ReturnsBadRequest(char type)
    {
        // Arrange.
        var model = new TransactionViewModel { TransactionType = type, AccountNumber = 4100, Amount = 10 };

        // Act.
        var result = await PostTransactionAsync(model);

        // Assert.
        Assert.IsType<BadRequestResult>(result);
    }

    [Theory]
    [InlineData(4100, 100, true)]      // savings down to exactly $0
    [InlineData(4100, 100.01, false)]  // savings below $0
    [InlineData(4101, 600, true)]      // checking down to exactly -$100
    [InlineData(4101, 600.01, false)]  // checking below -$100
    public async Task Transaction_Post_Withdraw_EnforcesMinimumBalance(int accountNumber, double amount, bool expectedValid)
    {
        // Arrange.
        var model = new TransactionViewModel
        {
            TransactionType = TransactionTypes.Withdraw, AccountNumber = accountNumber, Amount = (decimal)amount
        };

        // Act.
        var result = await PostTransactionAsync(model);

        // Assert.
        Assert.Equal(expectedValid, _controller.ModelState.IsValid);
        Assert.IsType(expectedValid ? typeof(RedirectToActionResult) : typeof(ViewResult), result);
    }

    [Theory]
    [InlineData(TransactionTypes.Withdraw, 1, 0)]     // second withdrawal is still free
    [InlineData(TransactionTypes.Withdraw, 2, 0.05)]  // third withdrawal is charged
    [InlineData(TransactionTypes.Transfer, 2, 0.10)]  // transfers cost more
    [InlineData(TransactionTypes.Deposit, 2, 0)]      // deposits are never charged
    public async Task Transaction_Post_ChargesServiceFeeAfterTwoFreeTransactions(char type, int previousWithdrawals, double expectedCharge)
    {
        // Arrange.
        for (var i = 0; i < previousWithdrawals; i++)
        {
            Context.Transactions.Add(new Transaction
            {
                TransactionType = TransactionTypes.Withdraw, AccountNumber = 4101, Amount = 1, TransactionTimeUtc = DateTime.UtcNow
            });
        }
        Context.SaveChanges();

        var model = new TransactionViewModel
        {
            TransactionType = type,
            AccountNumber = 4101,
            DestinationAccountNumber = type == TransactionTypes.Transfer ? 4100 : null,
            Amount = 10
        };

        // Act.
        await PostTransactionAsync(model);

        // Assert.
        Assert.Equal((decimal)expectedCharge, PendingTransaction.ServiceCharge);
    }

    [Fact]
    public void Confirm_NoPendingTransaction_RedirectsToIndex()
    {
        // Act.
        var result = _controller.Confirm();

        // Assert.
        Assert.Equal(nameof(CustomerController.Index), Assert.IsType<RedirectToActionResult>(result).ActionName);
    }

    [Fact]
    public void Confirm_WithPendingTransaction_ShowsIt()
    {
        // Arrange.
        _controller.HttpContext.Session.SetObject(PendingTransactionKey,
            new TransactionViewModel { TransactionType = TransactionTypes.Deposit, AccountNumber = 4100, Amount = 20 });

        // Act.
        var result = _controller.Confirm();

        // Assert.
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(20, Assert.IsType<TransactionViewModel>(view.Model).Amount);
    }

    [Fact]
    public async Task ConfirmPost_Deposit_UpdatesBalanceAndAddsTransaction()
    {
        // Arrange.
        _controller.HttpContext.Session.SetObject(PendingTransactionKey,
            new TransactionViewModel { TransactionType = TransactionTypes.Deposit, AccountNumber = 4100, Amount = 25.50m, Comment = "Pay" });

        // Act.
        var result = await _controller.ConfirmPost();

        // Assert.
        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(125.50m, Context.Accounts.Find(4100).Balance);
        var transaction = Assert.Single(Context.Transactions.Where(x => x.AccountNumber == 4100));
        Assert.Equal(TransactionTypes.Deposit, transaction.TransactionType);
        Assert.Null(PendingTransaction);
    }

    [Fact]
    public async Task ConfirmPost_TransferWithFee_MovesMoneyAndRecordsAllTransactions()
    {
        // Arrange.
        _controller.HttpContext.Session.SetObject(PendingTransactionKey, new TransactionViewModel
        {
            TransactionType = TransactionTypes.Transfer, AccountNumber = 4101, DestinationAccountNumber = 4200,
            Amount = 50, ServiceCharge = 0.10m
        });

        // Act.
        await _controller.ConfirmPost();

        // Assert.
        Assert.Equal(449.90m, Context.Accounts.Find(4101).Balance);  // 500 - 50 - 0.10
        Assert.Equal(100m, Context.Accounts.Find(4200).Balance);     // 50 + 50
        Assert.Equal(2, Context.Transactions.Count(x => x.AccountNumber == 4101));  // outgoing T + S
        var incoming = Assert.Single(Context.Transactions.Where(x => x.AccountNumber == 4200));
        Assert.Null(incoming.DestinationAccountNumber);
    }

    [Fact]
    public void Cancel_RemovesPendingTransactionAndRedirects()
    {
        // Arrange.
        _controller.HttpContext.Session.SetObject(PendingTransactionKey,
            new TransactionViewModel { TransactionType = TransactionTypes.Deposit, AccountNumber = 4100, Amount = 20 });

        // Act.
        var result = _controller.Cancel();

        // Assert.
        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(PendingTransaction);
        Assert.Equal(100m, Context.Accounts.Find(4100).Balance);
    }

    [Theory]
    [InlineData(1, 4)]
    [InlineData(2, 4)]
    [InlineData(3, 2)]
    public async Task Statements_ShowsFourPerPageNewestFirst(int page, int expectedCount)
    {
        // Arrange: 10 transactions, one minute apart.
        for (var i = 1; i <= 10; i++)
        {
            Context.Transactions.Add(new Transaction
            {
                TransactionType = TransactionTypes.Deposit, AccountNumber = 4100, Amount = i,
                TransactionTimeUtc = new DateTime(2026, 1, 1, 9, i, 0, DateTimeKind.Utc)
            });
        }
        Context.SaveChanges();

        // Act.
        var result = await _controller.Statements(4100, page);

        // Assert.
        var view = Assert.IsType<ViewResult>(result);
        var transactions = Assert.IsAssignableFrom<IPagedList<Transaction>>(view.Model);
        Assert.Equal(expectedCount, transactions.Count);
        Assert.Equal(transactions.OrderByDescending(x => x.TransactionTimeUtc), transactions);
    }

    [Theory]
    [InlineData(4100, 100)]  // savings: available = balance
    [InlineData(4101, 600)]  // checking: balance + $100 overdraft
    public async Task Statements_ShowsAvailableBalance(int accountNumber, double expected)
    {
        // Act.
        var result = await _controller.Statements(accountNumber);

        // Assert.
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal((decimal)expected, (decimal)view.ViewData["AvailableBalance"]);
    }

    [Fact]
    public async Task Statements_OtherCustomersAccount_ReturnsNotFound()
    {
        // Act.
        var result = await _controller.Statements(4200);

        // Assert.
        Assert.IsType<NotFoundResult>(result);
    }
}