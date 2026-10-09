using System.ComponentModel.DataAnnotations;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mcba.CustomerWebsite.ViewModels;

public class TransactionViewModel
{
    public char TransactionType { get; set; }

    [Display(Name = "Account")]
    public int AccountNumber { get; set; }

    [Display(Name = "Destination Account")]
    public int? DestinationAccountNumber { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive.")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [StringLength(30)]
    public string Comment { get; set;}

    // Set by the controller only, never from the posted form.
    [BindNever]
    [Display(Name = "Service Charge")]
    public decimal ServiceCharge { get; set; }

    public string TypeName => TransactionType switch
    {
        TransactionTypes.Withdraw => "Withdraw",
        TransactionTypes.Deposit => "Deposit",
        _ => "Deposit"
    };
}
