using MediQueue.Contracts.Dashboard;
using MediQueue.Web.Clients;
using System.Net;

namespace MediQueue.IntegrationTests.Web;

public sealed class FakeDashboardApiClient : IDashboardApiClient
{
    public Task<ApiClientResult<DashboardResponse>> GetDashboardAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        string[] tokenParts = accessToken.Split('|');
        string scenario = tokenParts.Length >= 3 ? tokenParts[^2] : string.Empty;
        string role = tokenParts.LastOrDefault() ?? "Patient";
        if (scenario == "dashboard401")
        {
            return Task.FromResult(ApiClientResult<DashboardResponse>.Failure(new ApiProblem(
                HttpStatusCode.Unauthorized, "Phiên không hợp lệ", "Vui lòng đăng nhập lại.",
                new Dictionary<string, string[]>())));
        }
        if (scenario == "dashboard403")
        {
            return Task.FromResult(ApiClientResult<DashboardResponse>.Failure(new ApiProblem(
                HttpStatusCode.Forbidden, "Không có quyền", "Tài khoản không có quyền truy cập.",
                new Dictionary<string, string[]>())));
        }
        if (scenario == "dashboardunavailable")
        {
            return Task.FromResult(ApiClientResult<DashboardResponse>.Failure(ApiProblem.Unavailable()));
        }
        DashboardAccountSummary account = new(
            $"Tài khoản {role}",
            $"{role.ToLowerInvariant()}@mediqueue.test",
            "0901234567",
            true);
        DashboardResponse response = role switch
        {
            "Admin" => new(role, account, new AdminDashboardResponse(
                new DashboardCountSummary(4, 4),
                new DashboardCountSummary(1, 1),
                new DashboardCountSummary(1, 1),
                new DashboardCountSummary(2, 2),
                new DashboardCountSummary(3, 2),
                [new("Admin", 1), new("Doctor", 1), new("Receptionist", 1), new("Patient", 1)]), null, null, null),
            "Doctor" => new(role, account, null, new DoctorDashboardResponse(
                true, Guid.NewGuid(), "Bác sĩ An", "TIM", "Tim mạch", true), null, null),
            "Receptionist" => new(role, account, null, null, new ReceptionistDashboardResponse(
                new DashboardCountSummary(1, 1),
                new DashboardCountSummary(1, 1),
                new DashboardCountSummary(2, 2),
                new DashboardCountSummary(3, 2)), null),
            _ => new("Patient", account, null, null, null, new PatientDashboardResponse(
                true, Guid.NewGuid(), "Nguyễn An", new DateOnly(1998, 5, 20), true))
        };
        return Task.FromResult(ApiClientResult<DashboardResponse>.Success(response));
    }
}
