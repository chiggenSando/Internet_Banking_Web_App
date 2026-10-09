using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mcba.Data.Models;

public class Login
{
    [Column(TypeName = "char")]
    [StringLength(8)]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "Login ID must be 8 digits.")]
    [Display(Name = "Login ID")]
    public string LoginID { get; set; }

    public int CustomerID { get; set; }
    public virtual Customer Customer { get; set; }

    [Column(TypeName = "char")]
    [Required, StringLength(94)]
    public string PasswordHash { get; set; }
}