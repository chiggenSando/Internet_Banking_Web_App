using System.ComponentModel.DataAnnotations;
using Mcba.Data.Models;

namespace Mcba.CustomerWebsite.ViewModels;

public class BillPayViewModel
{
    [Display(Name = "Account")]
    public int AccountNumber { get; set; }

    [Display(Name = "Payee")]
    public int PayeeID { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive.")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    // Entered in local time; the controller converts it to UTC for ScheduleTimeUtc.
    [Display(Name = "Scheduled Time")]
    public DateTime ScheduleTime { get; set; }

    public char Period { get; set; } = BillPayPeriods.OneOff;
}
