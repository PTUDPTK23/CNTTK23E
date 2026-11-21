using System.ComponentModel.DataAnnotations;
using MediQueue.Web.Models.ViewModels.Account;

namespace MediQueue.IntegrationTests.Web;

public sealed class AccountViewModelTests
{
    [Fact]
    public void Login_requires_valid_email_and_password()
    {
        var model = new LoginViewModel { Email = "not-an-email", Password = string.Empty };
        Assert.True(Validate(model).Count >= 2);
    }

    [Fact]
    public void Registration_requires_long_matching_password()
    {
        var model = new RegisterViewModel
        {
            FullName = "Nguyễn An",
            Email = "an@example.com",
            Password = "short",
            ConfirmPassword = "different"
        };

        List<ValidationResult> errors = Validate(model);
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterViewModel.Password)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
