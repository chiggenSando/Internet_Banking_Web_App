using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mcba.CustomerWebsite.Models;

public static class BillPayPeriods
{
    public const char OneOff = 'O';
    public const char Annual = 'A';
}

public class BillPay
{
    public int BillPayID { get; set; }

    [ForeignKey("Account")]
    [Display(Name = "Account Number")]
    public int AccountNumber { get; set; }
    public virtual Account Account { get; set; }

    public int PayeeID { get; set; }
    public virtual Payee Payee { get; set; }

    [Column(TypeName = "money")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive.")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [Display(Name = "Scheduled Time")]
    public DateTime ScheduleTimeUtc { get; set; }

    [Column(TypeName = "char(1)")]
    public char Period { get; set; }
}