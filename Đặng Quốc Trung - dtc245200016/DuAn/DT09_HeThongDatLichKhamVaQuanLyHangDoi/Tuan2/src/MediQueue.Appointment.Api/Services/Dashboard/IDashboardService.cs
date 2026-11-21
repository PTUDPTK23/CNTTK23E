using MediQueue.Contracts.Dashboard;

namespace MediQueue.Appointment.Api.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponse?> GetForUserAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken);
}
