using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MediQueue.Contracts.Catalogs;
using MediQueue.Domain.Common;
using MediQueue.Domain.Departments;
using MediQueue.Domain.Rooms;

namespace MediQueue.UnitTests.Catalogs;

public sealed class CatalogManagementTests
{
    [Fact]
    public void Department_normalizes_updates_and_toggles_state()
    {
        var item = new Department(Guid.NewGuid(), " timmach ", " Tim mạch ");
        Assert.Equal("TIMMACH", item.Code); Assert.Equal("Tim mạch", item.Name); Assert.True(item.IsActive);
        item.Update("noi", "Nội tổng hợp"); item.Deactivate();
        Assert.Equal("NOI", item.Code); Assert.False(item.IsActive);
        item.Activate(); Assert.True(item.IsActive);
    }

    [Fact]
    public void Room_normalizes_optional_location_and_toggles_state()
    {
        var item = new Room(Guid.NewGuid(), " p101 ", " Phòng 101 ", " Tầng 1 ");
        Assert.Equal("P101", item.Code); Assert.Equal("Tầng 1", item.Location);
        item.Update("p102", "Phòng 102", " "); item.Deactivate();
        Assert.Equal("P102", item.Code); Assert.Null(item.Location); Assert.False(item.IsActive);
        item.Activate(); Assert.True(item.IsActive);
    }

    [Fact]
    public void Domain_rejects_blank_and_overlong_values()
    {
        Assert.Throws<DomainRuleException>(() => new Department(Guid.NewGuid(), " ", "Tên"));
        Assert.Throws<DomainRuleException>(() => new Room(Guid.NewGuid(), "P1", new string('x', 201)));
    }

    [Fact]
    public void Contracts_enforce_code_format_and_limits()
    {
        ParameterInfo departmentCode = typeof(CreateDepartmentRequest).GetConstructors().Single().GetParameters()[0];
        ParameterInfo roomLocation = typeof(CreateRoomRequest).GetConstructors().Single().GetParameters()[2];
        Assert.NotNull(departmentCode.GetCustomAttribute<RegularExpressionAttribute>());
        Assert.Equal(32, departmentCode.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength);
        Assert.Equal(300, roomLocation.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength);
        Assert.Equal([10, 20, 50], DepartmentManagementValues.PageSizes);
        Assert.Contains("location", RoomManagementValues.SortFields);
    }
}
