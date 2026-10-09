using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mcba.Data.Models;

public static class AccountTypes
{
    public const char Checking = 'C';
    public const char Savings = 'S';
}

public class Account
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Range(1000, 9999, ErrorMessage = "Account number must be 4 digits.")]
    [Display(Name = "Account Number")]
    public int AccountNumber { get; set; }

    [Column(TypeName = "char(1)")]
    [Display(Name = "Type")]
    public char AccountType { get; set; }

    public int CustomerID { get; set; }
    public virtual Customer Customer { get; set; }

    [Column(TypeName = "money")]
    [DataType(DataType.Currency)]
    public decimal Balance { get; set; }

    [InverseProperty("Account")]
    public virtual List<Transaction> Transactions { get; set; }

    public virtual List<BillPay> BillPays { get; set; }
}