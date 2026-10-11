using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mcba.AdminApi.Tests;

// Builds a fresh, seeded in-memory McbaContext. Each call gets a unique database name so tests never share state.
public static class TestDatabase
{
    public const string UnlockedLoginID = "12345678";
    public const string LockedLoginID = "38074569";
    public const string MissingLoginID = "99999999";

    public static McbaContext Create()
    {
        var options = new DbContextOptionsBuilder<McbaContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new McbaContext(options);

        context.Customers.AddRange(
            new Customer { CustomerID = 2100, Name = "Matthew Bolger" },
            new Customer { CustomerID = 2200, Name = "Rodney Cocker" });
        context.Logins.AddRange(
            new Login { LoginID = UnlockedLoginID, CustomerID = 2100, PasswordHash = new string('x', 94) },
            new Login { LoginID = LockedLoginID, CustomerID = 2200, PasswordHash = new string('x', 94), IsLocked = true });
        context.Payees.AddRange(
            NewPayee("Telstra", "VIC"),
            NewPayee("AGL Energy", "NSW"),
            NewPayee("Yarra Valley Water", "VIC"));
        context.SaveChanges();

        // Clear tracked entities so tests read back from the "database" rather than the change tracker.
        context.ChangeTracker.Clear();
        return context;
    }

    public static Payee NewPayee(string name = "Origin Energy", string state = "VIC") => new()
    {
        Name = name,
        Address = "123 Collins Street",
        City = "Melbourne",
        State = state,
        Postcode = "3000",
        Phone = "(03) 9999 8888"
    };
}
