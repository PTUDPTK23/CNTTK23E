using System.Security.Claims;
using MediQueue.Contracts.Catalogs;

namespace MediQueue.Appointment.Api.Services.Catalogs;

public interface IDepartmentService
{
    Task<AccountServiceResult<PagedDepartmentsResponse>> GetAsync(DepartmentListQuery query, CancellationToken token);
    Task<AccountServiceResult<DepartmentDetailsResponse>> GetAsync(Guid id, CancellationToken token);
    Task<AccountServiceResult<DepartmentDetailsResponse>> CreateAsync(ClaimsPrincipal actor, CreateDepartmentRequest request, CancellationToken token);
    Task<AccountServiceResult<DepartmentDetailsResponse>> UpdateAsync(ClaimsPrincipal actor, Guid id, UpdateDepartmentRequest request, CancellationToken token);
    Task<AccountServiceResult> SetActiveAsync(ClaimsPrincipal actor, Guid id, bool active, CancellationToken token);
}
