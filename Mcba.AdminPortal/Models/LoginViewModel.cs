using System.ComponentModel.DataAnnotations;

namespace Mcba.AdminPortal.Models;

// Mirrors the LoginDto JSON returned by the Admin Web API.
public class LoginViewModel
{
    [Display(Name = "Login ID")]
    public string LoginID { get; set; }

    [Display(Name = "Customer ID")]
    public int CustomerID { get; set; }

    [Display(Name = "Customer")]
    public string CustomerName { get; set; }

    [Display(Name = "Locked")]
    public bool IsLocked { get; set; }
}
