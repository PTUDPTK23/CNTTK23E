using MediQueue.Domain.Security;

namespace MediQueue.UnitTests.Security;

public sealed class SystemRolesTests
{
    [Fact]
    public void System_defines_exactly_the_four_approved_roles()
    {
        string[] expected = ["Patient", "Doctor", "Receptionist", "Admin"];

        Assert.Equal(expected, SystemRoles.All);
        Assert.Equal(expected.Length, SystemRoles.All.Distinct(StringComparer.Ordinal).Count());
    }
}
