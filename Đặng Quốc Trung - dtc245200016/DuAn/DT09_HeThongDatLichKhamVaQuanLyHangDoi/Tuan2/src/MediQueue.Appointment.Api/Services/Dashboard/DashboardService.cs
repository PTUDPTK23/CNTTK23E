using MediQueue.Contracts.Dashboard;
using MediQueue.Domain.Security;
using MediQueue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MediQueue.Appointment.Api.Services.Dashboard;

public sealed class DashboardService(MediQueueDbContext dbContext) : IDashboardService
{
    public async Task<DashboardResponse?> GetForUserAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        AccountData? account = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new AccountData(
                user.Email!,
                user.PhoneNumber,
                user.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            return null;
        }

        return role switch
        {
            SystemRoles.Admin => await BuildAdminAsync(account, cancellationToken),
            SystemRoles.Doctor => await BuildDoctorAsync(userId, account, cancellationToken),
            SystemRoles.Receptionist => await BuildReceptionistAsync(account, cancellationToken),
            SystemRoles.Patient => await BuildPatientAsync(userId, account, cancellationToken),
            _ => null
        };
    }

    private async Task<DashboardResponse> BuildAdminAsync(
        AccountData account,
        CancellationToken cancellationToken)
    {
        DashboardCountSummary accounts = await CountUsersAsync(cancellationToken);
        DashboardCountSummary doctors = await CountDoctorsAsync(cancellationToken);
        DashboardCountSummary patients = await CountPatientsAsync(cancellationToken);
        DashboardCountSummary departments = await CountDepartmentsAsync(cancellationToken);
        DashboardCountSummary rooms = await CountRoomsAsync(cancellationToken);

        List<RoleAccountCount> storedCounts = await (
            from role in dbContext.Roles.AsNoTracking()
            where role.Name != null && SystemRoles.All.Contains(role.Name)
            select new RoleAccountCount(
                role.Name!,
                dbContext.UserRoles.Count(userRole => userRole.RoleId == role.Id)))
            .ToListAsync(cancellationToken);

        string[] roleOrder =
        [
            SystemRoles.Admin,
            SystemRoles.Doctor,
            SystemRoles.Receptionist,
            SystemRoles.Patient
        ];
        RoleAccountCount[] accountsByRole = roleOrder
            .Select(role => storedCounts.FirstOrDefault(item => item.Role == role)
                ?? new RoleAccountCount(role, 0))
            .ToArray();

        return new DashboardResponse(
            SystemRoles.Admin,
            ToAccountSummary(account, null),
            new AdminDashboardResponse(
                accounts,
                doctors,
                patients,
                departments,
                rooms,
                accountsByRole),
            null,
            null,
            null);
    }

    private async Task<DashboardResponse> BuildDoctorAsync(
        Guid userId,
        AccountData account,
        CancellationToken cancellationToken)
    {
        DoctorDashboardResponse? profile = await dbContext.Doctors
            .AsNoTracking()
            .Where(doctor => doctor.UserId == userId)
            .Select(doctor => new DoctorDashboardResponse(
                true,
                doctor.Id,
                doctor.DisplayName,
                doctor.Department.Code,
                doctor.Department.Name,
                doctor.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        profile ??= new DoctorDashboardResponse(false, null, null, null, null, null);

        return new DashboardResponse(
            SystemRoles.Doctor,
            ToAccountSummary(account, profile.DisplayName),
            null,
            profile,
            null,
            null);
    }

    private async Task<DashboardResponse> BuildReceptionistAsync(
        AccountData account,
        CancellationToken cancellationToken)
    {
        var response = new ReceptionistDashboardResponse(
            await CountDoctorsAsync(cancellationToken),
            await CountPatientsAsync(cancellationToken),
            await CountDepartmentsAsync(cancellationToken),
            await CountRoomsAsync(cancellationToken));

        return new DashboardResponse(
            SystemRoles.Receptionist,
            ToAccountSummary(account, null),
            null,
            null,
            response,
            null);
    }

    private async Task<DashboardResponse> BuildPatientAsync(
        Guid userId,
        AccountData account,
        CancellationToken cancellationToken)
    {
        PatientDashboardResponse? profile = await dbContext.Patients
            .AsNoTracking()
            .Where(patient => patient.UserId == userId)
            .Select(patient => new PatientDashboardResponse(
                true,
                patient.Id,
                patient.FullName,
                patient.DateOfBirth,
                patient.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        profile ??= new PatientDashboardResponse(false, null, null, null, null);

        return new DashboardResponse(
            SystemRoles.Patient,
            ToAccountSummary(account, profile.FullName),
            null,
            null,
            null,
            profile);
    }

    private Task<DashboardCountSummary> CountUsersAsync(CancellationToken cancellationToken) =>
        CountAsync(
            dbContext.Users.AsNoTracking().Select(user => new ActiveItem(user.IsActive)),
            cancellationToken);

    private Task<DashboardCountSummary> CountDoctorsAsync(CancellationToken cancellationToken) =>
        CountAsync(
            dbContext.Doctors.AsNoTracking().Select(doctor => new ActiveItem(doctor.IsActive)),
            cancellationToken);

    private Task<DashboardCountSummary> CountPatientsAsync(CancellationToken cancellationToken) =>
        CountAsync(
            dbContext.Patients.AsNoTracking().Select(patient => new ActiveItem(patient.IsActive)),
            cancellationToken);

    private Task<DashboardCountSummary> CountDepartmentsAsync(CancellationToken cancellationToken) =>
        CountAsync(
            dbContext.Departments.AsNoTracking().Select(department => new ActiveItem(department.IsActive)),
            cancellationToken);

    private Task<DashboardCountSummary> CountRoomsAsync(CancellationToken cancellationToken) =>
        CountAsync(
            dbContext.Rooms.AsNoTracking().Select(room => new ActiveItem(room.IsActive)),
            cancellationToken);

    private static async Task<DashboardCountSummary> CountAsync(
        IQueryable<ActiveItem> query,
        CancellationToken cancellationToken)
    {
        DashboardCountSummary? count = await query
            .GroupBy(_ => 1)
            .Select(group => new DashboardCountSummary(
                group.Count(),
                group.Count(item => item.IsActive)))
            .SingleOrDefaultAsync(cancellationToken);
        return count ?? new DashboardCountSummary(0, 0);
    }

    private static DashboardAccountSummary ToAccountSummary(
        AccountData account,
        string? profileName) => new(
            string.IsNullOrWhiteSpace(profileName)
                ? GetEmailAlias(account.Email)
                : profileName,
            account.Email,
            account.PhoneNumber,
            account.IsActive);

    private static string GetEmailAlias(string email)
    {
        int separator = email.IndexOf('@', StringComparison.Ordinal);
        return separator > 0 ? email[..separator] : email;
    }

    private sealed record AccountData(string Email, string? PhoneNumber, bool IsActive);

    private sealed record ActiveItem(bool IsActive);
}
