using Mcba.CustomerWebsite.Controllers;
using Mcba.CustomerWebsite.Tests.Common;
using Mcba.CustomerWebsite.Tests.Extensions;
using Mcba.CustomerWebsite.ViewModels;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Mcba.CustomerWebsite.Tests;

public class ProfileControllerTests : TestBase
{
    private readonly ProfileController _controller;

    public ProfileControllerTests()
    {
        _controller = WithLoggedInCustomer(new ProfileController(Context));
    }

    [Fact]
    public async Task Index_ReturnsViewWithCustomer()
    {
        // Act.
        var result = await _controller.Index();

        // Assert.
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Steve McSteve", Assert.IsType<Customer>(view.Model).Name);
    }

    [Fact]
    public async Task Edit_Get_ReturnsCurrentDetails()
    {
        // Act.
        var result = await _controller.Edit();

        // Assert.
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Steve McSteve", Assert.IsType<ProfileViewModel>(view.Model).Name);
    }

    [Fact]
    public async Task Edit_Post_Valid_SavesChangesAndUpdatesSessionName()
    {
        // Arrange.
        var model = new ProfileViewModel
        {
            Name = "Steve McSteve", TFN = "123 456 789", Address = "111 Smith St", City = "Melbourne",
            State = "VIC", Postcode = "3000", Mobile = "0412 345 678"
        };
        _controller.ModelState.ValidateObject(model);

        // Act.
        var result = await _controller.Edit(model);

        // Assert.
        Assert.IsType<RedirectToActionResult>(result);
        var customer = Context.Customers.Find(CustomerID);
        Assert.Equal("Steve McSteve", customer.Name);
        Assert.Equal("123 456 789", customer.TFN);
        Assert.Equal("0412 345 678", customer.Mobile);
        Assert.Equal("Steve McSteve", _controller.HttpContext.Session.GetString(nameof(Customer.Name)));
    }

    [Theory]
    [InlineData(null, "123 456 789", "VIC", "3000", "0412 345 678", nameof(ProfileViewModel.Name))]
    [InlineData("Steve McSteve", "123456789", "VIC", "3000", "0412 345 678", nameof(ProfileViewModel.TFN))]
    [InlineData("Steve McSteve", "123 456 789", "XYZ", "3000", "0412 345 678", nameof(ProfileViewModel.State))]
    [InlineData("Steve McSteve", "123 456 789", "VIC", "300", "0412 345 678", nameof(ProfileViewModel.Postcode))]
    [InlineData("Steve McSteve", "123 456 789", "VIC", "3000", "0312 345 678", nameof(ProfileViewModel.Mobile))]
    public async Task Edit_Post_InvalidFormat_ReturnsViewAndDoesNotSave(
        string name, string tfn, string state, string postcode, string mobile, string expectedErrorField)
    {
        // Arrange.
        var model = new ProfileViewModel { Name = name, TFN = tfn, State = state, Postcode = postcode, Mobile = mobile };
        _controller.ModelState.ValidateObject(model);

        // Act.
        var result = await _controller.Edit(model);

        // Assert.
        Assert.IsType<ViewResult>(result);
        Assert.True(_controller.ModelState.ContainsKey(expectedErrorField));
        Assert.Equal("Steve McSteve", Context.Customers.Find(CustomerID).Name);
    }

    [Fact]
    public async Task ChangePassword_Valid_StoresNewHash()
    {
        // Arrange.
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = Password, NewPassword = "newPassword1", ConfirmPassword = "newPassword1"
        };
        _controller.ModelState.ValidateObject(model);

        // Act.
        var result = await _controller.ChangePassword(model);

        // Assert.
        Assert.IsType<RedirectToActionResult>(result);
        var login = Context.Logins.Find(LoginID);
        Assert.True(s_simpleHash.Verify("newPassword1", login.PasswordHash));
        Assert.False(s_simpleHash.Verify(Password, login.PasswordHash));
    }

    [Theory]
    [InlineData("wrongPassword", "newPassword1", "newPassword1", nameof(ChangePasswordViewModel.CurrentPassword))]
    [InlineData(Password, "newPassword1", "different1", nameof(ChangePasswordViewModel.ConfirmPassword))]
    public async Task ChangePassword_Invalid_ReturnsViewAndKeepsOldPassword(
        string current, string newPassword, string confirm, string expectedErrorField)
    {
        // Arrange.
        var model = new ChangePasswordViewModel { CurrentPassword = current, NewPassword = newPassword, ConfirmPassword = confirm };
        _controller.ModelState.ValidateObject(model);

        // Act.
        var result = await _controller.ChangePassword(model);

        // Assert.
        Assert.IsType<ViewResult>(result);
        Assert.True(_controller.ModelState.ContainsKey(expectedErrorField));
        Assert.True(s_simpleHash.Verify(Password, Context.Logins.Find(LoginID).PasswordHash));
    }
}