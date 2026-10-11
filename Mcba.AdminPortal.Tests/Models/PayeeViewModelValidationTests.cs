using System.ComponentModel.DataAnnotations;
using Mcba.AdminPortal.Models;
using Mcba.AdminPortal.Tests.Controllers;

namespace Mcba.AdminPortal.Tests.Models;

// The portal validates the payee form itself before calling the API, using these annotations.
public class PayeeViewModelValidationTests
{
    [Fact]
    public void ValidPayee_PassesValidation()
    {
        Assert.Empty(Validate(PayeesControllerTests.NewPayee()));
    }

    [Theory]
    [InlineData("VIC", true)]
    [InlineData("NT", true)]
    [InlineData("vic", false)]
    [InlineData("VICT", false)]
    [InlineData("XX", false)]
    public void State_MustBeAnAustralianState(string state, bool valid)
    {
        var payee = PayeesControllerTests.NewPayee();
        payee.State = state;

        AssertValid(valid, payee, nameof(PayeeViewModel.State));
    }

    [Theory]
    [InlineData("300", false)]
    [InlineData("3000", true)]
    [InlineData("30000", false)]
    [InlineData("30a0", false)]
    public void Postcode_MustBeFourDigits(string postcode, bool valid)
    {
        var payee = PayeesControllerTests.NewPayee();
        payee.Postcode = postcode;

        AssertValid(valid, payee, nameof(PayeeViewModel.Postcode));
    }

    [Theory]
    [InlineData("(03) 9999 8888", true)]
    [InlineData("03 9999 8888", false)]
    [InlineData("(03) 999 8888", false)]
    [InlineData("(13) 9999 8888", false)]
    public void Phone_MustMatchAustralianFormat(string phone, bool valid)
    {
        var payee = PayeesControllerTests.NewPayee();
        payee.Phone = phone;

        AssertValid(valid, payee, nameof(PayeeViewModel.Phone));
    }

    [Theory]
    [InlineData(nameof(PayeeViewModel.Name), 50, true)]
    [InlineData(nameof(PayeeViewModel.Name), 51, false)]
    [InlineData(nameof(PayeeViewModel.Address), 50, true)]
    [InlineData(nameof(PayeeViewModel.Address), 51, false)]
    [InlineData(nameof(PayeeViewModel.City), 40, true)]
    [InlineData(nameof(PayeeViewModel.City), 41, false)]
    public void TextFields_RespectMaximumLength(string property, int length, bool valid)
    {
        var payee = PayeesControllerTests.NewPayee();
        typeof(PayeeViewModel).GetProperty(property)!.SetValue(payee, new string('a', length));

        AssertValid(valid, payee, property);
    }

    [Theory]
    [InlineData(nameof(PayeeViewModel.Name))]
    [InlineData(nameof(PayeeViewModel.Address))]
    [InlineData(nameof(PayeeViewModel.City))]
    [InlineData(nameof(PayeeViewModel.State))]
    [InlineData(nameof(PayeeViewModel.Postcode))]
    [InlineData(nameof(PayeeViewModel.Phone))]
    public void RequiredField_Missing_FailsValidation(string property)
    {
        var payee = PayeesControllerTests.NewPayee();
        typeof(PayeeViewModel).GetProperty(property)!.SetValue(payee, null);

        AssertValid(false, payee, property);
    }

    [Theory]
    [InlineData("admin", "admin", true)]
    [InlineData("", "admin", false)]
    [InlineData("admin", "", false)]
    public void AdminLogin_RequiresUsernameAndPassword(string username, string password, bool valid)
    {
        var model = new AdminLoginViewModel { Username = username, Password = password };
        var results = new List<ValidationResult>();

        Assert.Equal(valid, Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true));
    }

    private static void AssertValid(bool valid, PayeeViewModel payee, string property)
    {
        var failedMembers = Validate(payee).SelectMany(r => r.MemberNames);
        Assert.Equal(!valid, failedMembers.Contains(property));
    }

    private static List<ValidationResult> Validate(PayeeViewModel payee)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(payee, new ValidationContext(payee), results, validateAllProperties: true);
        return results;
    }
}
