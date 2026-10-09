using System.ComponentModel.DataAnnotations;

namespace Mcba.Data.Models;

public class Payee
{
    public int PayeeID { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; }

    [Required, StringLength(50)]
    public string Address { get; set; }

    [Required, StringLength(40)]
    public string City { get; set; }

    [Required, StringLength(3)]
    [RegularExpression("^(NSW|VIC|QLD|WA|SA|TAS|ACT|NT)$", ErrorMessage = "State must be an Australian state, e.g. VIC.")]
    public string State { get; set; }

    [Required, StringLength(4)]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Postcode must be 4 digits.")]
    public string Postcode { get; set; }

    [Required, StringLength(14)]
    [RegularExpression(@"^\(0\d\) \d{4} \d{4}$", ErrorMessage = "Phone must be in the format (0X) XXXX XXXX.")]
    public string Phone { get; set; }

    public virtual List<BillPay> BillPays { get; set; }
}