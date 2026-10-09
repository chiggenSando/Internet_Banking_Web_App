namespace Mcba.Data.Dtos;

public class LoginDto
{
    public string LoginID { get; set; }
    public int CustomerID { get; set; }
    public string CustomerName { get; set; }
    public bool IsLocked { get; set; }
}
