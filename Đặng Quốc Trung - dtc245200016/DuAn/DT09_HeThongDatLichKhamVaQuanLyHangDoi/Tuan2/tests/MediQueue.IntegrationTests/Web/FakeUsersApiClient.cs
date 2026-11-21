using System.Net;
using MediQueue.Contracts.Users;
using MediQueue.Web.Clients;

namespace MediQueue.IntegrationTests.Web;

public sealed class FakeUsersApiClient : IUsersApiClient
{
    public static readonly Guid UserId = Guid.Parse("bc695646-6766-4149-9d90-0a37d7cc4bee");
    public FakeUsersApiScenario Scenario { get; set; }

    public void Reset() => Scenario = FakeUsersApiScenario.Success;

    private static UserDetailsResponse User => new(UserId, "Bác sĩ An", "doctor@example.test", "0900000000", "Doctor", true, false, null, DateTimeOffset.UtcNow, false);
    public Task<ApiClientResult<PagedUsersResponse>> GetUsersAsync(string t, UserListQuery q, CancellationToken c) =>
        Task.FromResult(Scenario switch
        {
            FakeUsersApiScenario.ServerError => ApiClientResult<PagedUsersResponse>.Failure(new(
                HttpStatusCode.InternalServerError,
                "Lỗi máy chủ",
                "Chi tiết nội bộ không được hiển thị.",
                new Dictionary<string, string[]>())),
            FakeUsersApiScenario.Unavailable => ApiClientResult<PagedUsersResponse>.Failure(ApiProblem.Unavailable()),
            FakeUsersApiScenario.InvalidResponse => ApiClientResult<PagedUsersResponse>.Failure(ApiProblem.InvalidResponse()),
            FakeUsersApiScenario.BadRequest => Failure(HttpStatusCode.BadRequest, "Yêu cầu chưa hợp lệ"),
            FakeUsersApiScenario.NotFound => Failure(HttpStatusCode.NotFound, "Không tìm thấy tài khoản"),
            FakeUsersApiScenario.Conflict => Failure(HttpStatusCode.Conflict, "Dữ liệu vừa được thay đổi"),
            _ => ApiClientResult<PagedUsersResponse>.Success(new(
                [new(UserId, "Bác sĩ An", "doctor@example.test", "Doctor", true, false, DateTimeOffset.UtcNow)],
                1,
                10,
                1,
                1,
                new(2, 2, 0, 0, 1, 0)))
        });

    private static ApiClientResult<PagedUsersResponse> Failure(HttpStatusCode statusCode, string title) =>
        ApiClientResult<PagedUsersResponse>.Failure(new(
            statusCode,
            title,
            "Nội dung kiểm thử an toàn.",
            new Dictionary<string, string[]>()));

    public Task<ApiClientResult<UserDetailsResponse>> GetUserAsync(string t, Guid id, CancellationToken c) => Task.FromResult(ApiClientResult<UserDetailsResponse>.Success(User));
    public Task<ApiClientResult<UserDetailsResponse>> CreateAsync(string t, CreateStaffUserRequest r, CancellationToken c) => Task.FromResult(ApiClientResult<UserDetailsResponse>.Success(User));
    public Task<ApiClientResult<UserDetailsResponse>> UpdateAsync(string t, Guid id, UpdateStaffUserRequest r, CancellationToken c) => Task.FromResult(ApiClientResult<UserDetailsResponse>.Success(User));
    public Task<ApiClientResult> DeactivateAsync(string t, Guid id, CancellationToken c) => Task.FromResult(ApiClientResult.Success());
    public Task<ApiClientResult> ActivateAsync(string t, Guid id, CancellationToken c) => Task.FromResult(ApiClientResult.Success());
    public Task<ApiClientResult> UnlockAsync(string t, Guid id, CancellationToken c) => Task.FromResult(ApiClientResult.Success());
}

public enum FakeUsersApiScenario
{
    Success,
    ServerError,
    Unavailable,
    InvalidResponse,
    BadRequest,
    NotFound,
    Conflict
}
