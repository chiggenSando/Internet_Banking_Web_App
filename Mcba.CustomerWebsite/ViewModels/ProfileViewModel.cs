using System.ComponentModel.DataAnnotations;

namespace Mcba.CustomerWebsite.ViewModels;

// Editable Customer fields with the same format rules as Customer Model.
public class ProfileViewModel
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; }

    [StringLength(11)]
    [RegularExpression(@"^\d{3} \d{3} \d{3}$", ErrorMessage = "TFN must be in the format XXX XXX XXX.")]
    public string TFN { get; set; }

    [StringLength(50)]
    public string Address { get; set; }

    [StringLength(40)]
    public string City { get; set; }

    [StringLength(3)]
    [RegularExpression("^(NSW|VIC|QLD|WA|SA|TAS|ACT|NT)$", ErrorMessage = "State must be an Australian state, e.g. VIC.")]
    public string State { get; set; }

    [StringLength(4)]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Postcode must be 4 digits.")]
    public string Postcode { get; set; }

    [StringLength(12)]
    [RegularExpression(@"^04\d{2} \d{3} \d{3}$", ErrorMessage = "Mobile must be in the format 04XX XXX XXX.")]
    public string Mobile { get; set; }
}