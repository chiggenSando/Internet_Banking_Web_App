using System.ComponentModel.DataAnnotations;

namespace Mcba.CustomerWebsite.ViewModels;

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Current Password")]
    public string CurrentPassword { get; set; }

    [Required, DataType(DataType.Password), Display(Name = "New Password")]
    public string NewPassword { get; set; }

    [Required, DataType(DataType.Password), Display(Name = "Confirm New Password")]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; }
}