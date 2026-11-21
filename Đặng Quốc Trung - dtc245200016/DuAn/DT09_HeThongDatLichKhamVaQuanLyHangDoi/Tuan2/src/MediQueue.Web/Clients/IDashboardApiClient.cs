using MediQueue.Contracts.Dashboard;

namespace MediQueue.Web.Clients;

public interface IDashboardApiClient
{
    Task<ApiClientResult<DashboardResponse>> GetDashboardAsync(
        string accessToken,
        CancellationToken cancellationToken);
}
