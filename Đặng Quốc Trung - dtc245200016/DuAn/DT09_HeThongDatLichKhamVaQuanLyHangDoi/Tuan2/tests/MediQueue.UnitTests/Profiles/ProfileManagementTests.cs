using System.ComponentModel.DataAnnotations;
using MediQueue.Contracts.Profiles;
using MediQueue.Domain.Common;
using MediQueue.Domain.Doctors;
using MediQueue.Domain.Patients;

namespace MediQueue.UnitTests.Profiles;

public sealed class ProfileManagementTests
{
    [Fact]
    public void Doctor_update_profile_trims_name_and_changes_department()
    {
        var doctor = new Doctor(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Bác sĩ An");
        Guid departmentId = Guid.NewGuid();

        doctor.UpdateProfile("  BS. Nguyễn An  ", departmentId);

        Assert.Equal("BS. Nguyễn An", doctor.DisplayName);
        Assert.Equal(departmentId, doctor.DepartmentId);
    }

    [Fact]
    public void Doctor_rejects_invalid_profile_values()
    {
        var doctor = new Doctor(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Bác sĩ An");
        Assert.Throws<DomainRuleException>(() => doctor.UpdateProfile(" ", Guid.NewGuid()));
        Assert.Throws<DomainRuleException>(() => doctor.UpdateProfile("Bác sĩ An", Guid.Empty));
        Assert.Throws<DomainRuleException>(() => doctor.UpdateProfile(new string('x', 201), Guid.NewGuid()));
    }

    [Fact]
    public void Patient_updates_only_administrative_profile()
    {
        var patient = new Patient(Guid.NewGuid(), Guid.NewGuid(), "Nguyễn An");
        var birthDate = new DateOnly(1998, 5, 20);

        patient.UpdateAdministrativeProfile("  Nguyễn Minh An ", " 0901234567 ", birthDate);

        Assert.Equal("Nguyễn Minh An", patient.FullName);
        Assert.Equal("0901234567", patient.PhoneNumber);
        Assert.Equal(birthDate, patient.DateOfBirth);
    }

    [Fact]
    public void Contracts_define_bounded_paging_and_required_row_version()
    {
        Assert.Equal([10, 20, 50], DoctorManagementValues.PageSizes);
        Assert.Contains("department", DoctorManagementValues.SortFields);
        Assert.Equal([10, 20, 50], PatientManagementValues.PageSizes);
        Assert.NotNull(typeof(UpdateDoctorProfileRequest).GetProperty(nameof(UpdateDoctorProfileRequest.RowVersion))!
            .GetCustomAttributes(typeof(RequiredAttribute), true).SingleOrDefault());
        Assert.NotNull(typeof(UpdatePatientProfileRequest).GetProperty(nameof(UpdatePatientProfileRequest.RowVersion))!
            .GetCustomAttributes(typeof(RequiredAttribute), true).SingleOrDefault());
    }
}
