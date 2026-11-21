using MediQueue.Web.Presentation;

namespace MediQueue.UnitTests.Presentation;

public sealed class RolePresentationTests
{
    [Theory]
    [InlineData("Admin", "Quản trị viên", "admin")]
    [InlineData("Doctor", "Bác sĩ", "doctor")]
    [InlineData("Receptionist", "Lễ tân", "receptionist")]
    [InlineData("Patient", "Bệnh nhân", "patient")]
    public void Known_role_has_consistent_vietnamese_label_and_accent(
        string role,
        string expectedLabel,
        string expectedAccent)
    {
        Assert.Equal(expectedLabel, RolePresentation.GetLabel(role));
        Assert.Equal(expectedAccent, RolePresentation.GetAccentClass(role));
        Assert.False(string.IsNullOrWhiteSpace(RolePresentation.GetWorkspaceLabel(role)));
    }

    [Fact]
    public void Unknown_role_uses_safe_default_presentation()
    {
        Assert.Equal("Tài khoản", RolePresentation.GetLabel("Owner"));
        Assert.Equal("default", RolePresentation.GetAccentClass("Owner"));
    }
}
