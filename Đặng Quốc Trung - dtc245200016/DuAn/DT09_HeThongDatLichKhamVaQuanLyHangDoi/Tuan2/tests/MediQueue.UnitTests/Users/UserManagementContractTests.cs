using System.ComponentModel.DataAnnotations;
using MediQueue.Contracts.Users;

namespace MediQueue.UnitTests.Users;

public sealed class UserManagementContractTests
{
    [Fact]
    public void Create_staff_requires_valid_name_email_role_and_password()
    {
        var request = new CreateStaffUserRequest { FullName = "A", Email = "invalid", Role = "Admin", Password = "short" };
        var results = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), results, true));
        Assert.Contains(results, item => item.MemberNames.Contains(nameof(request.FullName)));
        Assert.Contains(results, item => item.MemberNames.Contains(nameof(request.Email)));
        Assert.Contains(results, item => item.MemberNames.Contains(nameof(request.Password)));
        Assert.DoesNotContain("Admin", UserManagementValues.StaffRoles);
        Assert.Equal([10, 20, 50], UserManagementValues.PageSizes);
    }

    [Theory]
    [InlineData("Doctor")]
    [InlineData("Receptionist")]
    public void Staff_roles_are_explicitly_allowed(string role) => Assert.Contains(role, UserManagementValues.StaffRoles);
}
