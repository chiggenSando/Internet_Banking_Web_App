using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mcba.Data.Models;

public static class TransactionTypes
{
    public const char Deposit = 'D';
    public const char Withdraw = 'W';
    public const char Transfer = 'T';
    public const char ServiceCharge = 'S';
    public const char BillPay = 'B';
}

public class Transaction
{   [Display(Name = "ID")]
    public int TransactionID { get; set; }

    [Column(TypeName = "char(1)")]
    [Display(Name = "Type")]
    public char TransactionType { get; set; }

    [ForeignKey("Account")]
    [Display(Name = "Account Number")]
    public int AccountNumber { get; set; }
    public virtual Account Account { get; set; }

    [ForeignKey("DestinationAccount")]
    [Display(Name = "Destination Account")]
    public int? DestinationAccountNumber { get; set; }
    public virtual Account DestinationAccount { get; set; }

    [Column(TypeName = "money")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive.")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [StringLength(30)]
    [Display(Name = "Comment")]
    public string Comment { get; set; }

    [Display(Name = "Time")]
    public DateTime TransactionTimeUtc { get; set; }
}

