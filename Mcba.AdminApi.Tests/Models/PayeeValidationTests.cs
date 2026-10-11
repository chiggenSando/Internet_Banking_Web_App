using System.ComponentModel.DataAnnotations;
using Mcba.Data.Models;

namespace Mcba.AdminApi.Tests.Models;

// [ApiController] rejects POST /api/payees with a 400 when these annotations fail. Calling the controller
// directly skips that pipeline, so the rules are tested here with the same validator MVC uses.
public class PayeeValidationTests
{
    [Fact]
    public void ValidPayee_PassesValidation()
    {
        Assert.Empty(Validate(TestDatabase.NewPayee()));
    }

    [Theory]
    [InlineData("VIC", true)]
    [InlineData("NT", true)]
    [InlineData("vic", false)]
    [InlineData("VICT", false)]
    [InlineData("XX", false)]
    [InlineData("", false)]
    public void State_MustBeAnAustralianState(string state, bool valid)
    {
        AssertValid(valid, TestDatabase.NewPayee(state: state), nameof(Payee.State));
    }

    [Theory]
    [InlineData("300", false)]
    [InlineData("3000", true)]
    [InlineData("30000", false)]
    [InlineData("30a0", false)]
    public void Postcode_MustBeFourDigits(string postcode, bool valid)
    {
        var payee = TestDatabase.NewPayee();
        payee.Postcode = postcode;

        AssertValid(valid, payee, nameof(Payee.Postcode));
    }

    [Theory]
    [InlineData("(03) 9999 8888", true)]
    [InlineData("03 9999 8888", false)]
    [InlineData("(03) 999 8888", false)]
    [InlineData("(13) 9999 8888", false)]
    [InlineData("(03) 9999 88888", false)]
    public void Phone_MustMatchAustralianFormat(string phone, bool valid)
    {
        var payee = TestDatabase.NewPayee();
        payee.Phone = phone;

        AssertValid(valid, payee, nameof(Payee.Phone));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void Name_IsLimitedTo50Characters(int length, bool valid)
    {
        AssertValid(valid, TestDatabase.NewPayee(name: new string('a', length)), nameof(Payee.Name));
    }

    [Theory]
    [InlineData(nameof(Payee.Name))]
    [InlineData(nameof(Payee.Address))]
    [InlineData(nameof(Payee.City))]
    [InlineData(nameof(Payee.State))]
    [InlineData(nameof(Payee.Postcode))]
    [InlineData(nameof(Payee.Phone))]
    public void RequiredField_Missing_FailsValidation(string property)
    {
        var payee = TestDatabase.NewPayee();
        typeof(Payee).GetProperty(property)!.SetValue(payee, null);

        AssertValid(false, payee, property);
    }

    private static void AssertValid(bool valid, Payee payee, string property)
    {
        var failedMembers = Validate(payee).SelectMany(r => r.MemberNames);
        Assert.Equal(!valid, failedMembers.Contains(property));
    }

    private static List<ValidationResult> Validate(Payee payee)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(payee, new ValidationContext(payee), results, validateAllProperties: true);
        return results;
    }
}
