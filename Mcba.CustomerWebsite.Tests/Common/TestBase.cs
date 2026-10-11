using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SimpleHashing.Net;

namespace Mcba.CustomerWebsite.Tests.Common;

// Shared Arrange step: a fresh in-memory SQLite database for every test (W10 approach).
public abstract class TestBase : IDisposable
{
    protected const int CustomerID = 2100;
    protected const string LoginID = "12345678";
    protected const string Password = "abc123";
    protected static readonly ISimpleHash s_simpleHash = new SimpleHash();

    private readonly SqliteConnection _connection;

    protected McbaContext Context { get; }

    protected TestBase()
    {
        // The in-memory database lives as long as this connection is open.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // SQLite has no len(); add it so the Login check constraints still run.
        _connection.CreateFunction<string, int>("len", value => value.TrimEnd().Length);

        Context = new McbaContext(new DbContextOptionsBuilder<McbaContext>().UseSqlite(_connection).Options);
        Context.Database.EnsureCreated();

        Context.Customers.AddRange(
            new Customer { CustomerID = 2100, Name = "Steve McSteve" },
            new Customer { CustomerID = 2200, Name = "Steve McSteve" });
        Context.Logins.Add(new Login { LoginID = LoginID, CustomerID = CustomerID, PasswordHash = s_simpleHash.Compute(Password) });
        Context.Accounts.AddRange(
            new Account { AccountNumber = 4100, AccountType = AccountTypes.Savings, CustomerID = 2100, Balance = 100 },
            new Account { AccountNumber = 4101, AccountType = AccountTypes.Checking, CustomerID = 2100, Balance = 500 },
            new Account { AccountNumber = 4200, AccountType = AccountTypes.Savings, CustomerID = 2200, Balance = 50 });
        Context.SaveChanges();
    }

    // Gives a controller what MVC gives it at runtime: an HttpContext with a session, and TempData.
    protected static T WithSession<T>(T controller) where T : Controller
    {
        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        return controller;
    }

    // As above, with customer 2100 logged in (the values LoginController stores).
    protected static T WithLoggedInCustomer<T>(T controller) where T : Controller
    {
        WithSession(controller);
        controller.HttpContext.Session.SetInt32(nameof(Customer.CustomerID), CustomerID);
        controller.HttpContext.Session.SetString(nameof(Customer.Name), "Steve McSteve");
        return controller;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}