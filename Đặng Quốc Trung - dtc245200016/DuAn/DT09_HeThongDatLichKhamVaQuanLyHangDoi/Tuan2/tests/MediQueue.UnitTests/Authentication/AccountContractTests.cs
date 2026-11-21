using System.ComponentModel.DataAnnotations;
using MediQueue.Contracts.Authentication;

namespace MediQueue.UnitTests.Authentication;

public sealed class AccountContractTests
{
    [Fact]
    public void Register_rejects_invalid_email_short_password_and_mismatch()
    {
        var request = new RegisterPatientRequest
        {
            FullName = "Bệnh nhân kiểm thử",
            Email = "khong-phai-email",
            Password = "ngan",
            ConfirmPassword = "khac"
        };

        List<ValidationResult> results = Validate(request);

        Assert.Contains(results, item => item.MemberNames.Contains(nameof(request.Email)));
        Assert.Contains(results, item => item.MemberNames.Contains(nameof(request.Password)));
        Assert.Contains(results, item => item.MemberNames.Contains(nameof(request.ConfirmPassword)));
    }

    [Fact]
    public void Register_accepts_valid_patient_contract()
    {
        var request = new RegisterPatientRequest
        {
            FullName = "Đặng Quốc Trung",
            Email = "patient@example.test",
            Password = "cau mat khau rat dai 2026",
            ConfirmPassword = "cau mat khau rat dai 2026",
            PhoneNumber = "0901234567"
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Public_registration_contract_cannot_request_a_privileged_role()
    {
        string[] properties = typeof(RegisterPatientRequest)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("Role", properties);
        Assert.DoesNotContain("IsActive", properties);
        Assert.DoesNotContain("IsAdmin", properties);
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            results,
            validateAllProperties: true);
        return results;
    }
}
