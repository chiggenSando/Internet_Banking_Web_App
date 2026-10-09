using Mcba.CustomerWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace Mcba.CustomerWebsite.Data;

public class McbaContext : DbContext
{
    public McbaContext(DbContextOptions<McbaContext> options) : base(options){}

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Login> Logins { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Payee> Payees { get; set; }
    public DbSet<BillPay> BillPays { get; set; }

    // Fluent API: check constraints cannot be expressed with data annotations.
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Login>().ToTable(b =>
        {
            b.HasCheckConstraint("CK_Login_LoginID", "len(LoginID) = 8");
            b.HasCheckConstraint("CK_Login_PasswordHash", "len(PasswordHash) = 94");
        });
        builder.Entity<Account>().ToTable(b =>
        {
            b.HasCheckConstraint("CK_Account_AccountType", "AccountType in ('C', 'S')");
            b.HasCheckConstraint("CK_Account_Balance",
                "AccountType = 'S' and Balance >= 0 or AccountType = 'C' and Balance >= -100");
        });
        builder.Entity<Transaction>().ToTable(b =>
        {
            b.HasCheckConstraint("CK_Transaction_TransactionType", "TransactionType in ('D', 'W', 'T', 'S', 'B')");
            b.HasCheckConstraint("CK_Transaction_Amount", "Amount > 0");
            b.HasCheckConstraint("CK_Transaction_OnlyTransferCanSetDestination",
                "TransactionType = 'T' or DestinationAccountNumber is null");
            b.HasCheckConstraint("CK_Transaction_CannotTransferToSelf",
                "DestinationAccountNumber is null or DestinationAccountNumber != AccountNumber");
        });
        builder.Entity<BillPay>().ToTable(b =>
        {
            b.HasCheckConstraint("CK_BillPay_Amount", "Amount > 0");
            b.HasCheckConstraint("CK_BillPay_Period", "Period in ('O', 'A')");
        });
    }
}