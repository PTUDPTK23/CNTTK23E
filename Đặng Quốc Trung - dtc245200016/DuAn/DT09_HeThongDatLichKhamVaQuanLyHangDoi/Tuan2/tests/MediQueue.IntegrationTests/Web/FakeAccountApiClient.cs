using System.Net;
using MediQueue.Contracts.Authentication;
using MediQueue.Web.Clients;

namespace MediQueue.IntegrationTests.Web;

public sealed class FakeAccountApiClient : IAccountApiClient
{
    public const string SecretToken = "integration-jwt-must-never-reach-browser";
    public static readonly Guid UserId = Guid.Parse("8b7052ef-6f7f-4ae3-b67f-c96ac8df14fa");

    public bool RejectLogin { get; set; }
    public bool BackendUnavailable { get; set; }
    public int LogoutCalls { get; private set; }
    public UpdateProfileRequest? LastProfileUpdate { get; private set; }
    public ChangePasswordRequest? LastPasswordChange { get; private set; }

    public void Reset()
    {
        RejectLogin = false;
        BackendUnavailable = false;
        LogoutCalls = 0;
        LastProfileUpdate = null;
        LastPasswordChange = null;
    }

    public Task<ApiClientResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (BackendUnavailable)
        {
            return Task.FromResult(ApiClientResult<LoginResponse>.Failure(ApiProblem.Unavailable()));
        }

        if (RejectLogin || string.Equals(request.Email, "unknown@example.com", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(ApiClientResult<LoginResponse>.Failure(new ApiProblem(
                HttpStatusCode.Unauthorized,
                "Đăng nhập không thành công",
                "Email hoặc mật khẩu không chính xác.",
                new Dictionary<string, string[]>())));
        }

        return Task.FromResult(ApiClientResult<LoginResponse>.Success(new LoginResponse(
            SecretToken,
            "Bearer",
            DateTimeOffset.UtcNow.AddMinutes(30),
            UserId,
            "Nguyễn An",
            request.Email,
            "Patient")));
    }

    public Task<ApiClientResult<AccountProfileResponse>> RegisterAsync(
        RegisterPatientRequest request,
        CancellationToken cancellationToken) => Task.FromResult(
            ApiClientResult<AccountProfileResponse>.Success(Profile(request.Email, request.FullName)));

    public Task<ApiClientResult<AccountProfileResponse>> GetProfileAsync(
        string accessToken,
        CancellationToken cancellationToken) => Task.FromResult(
            accessToken == SecretToken
                ? ApiClientResult<AccountProfileResponse>.Success(Profile())
                : ApiClientResult<AccountProfileResponse>.Failure(Unauthorized()));

    public Task<ApiClientResult<AccountProfileResponse>> UpdateProfileAsync(
        string accessToken,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        LastProfileUpdate = request;
        return Task.FromResult(ApiClientResult<AccountProfileResponse>.Success(
            Profile(fullName: request.FullName ?? "Nguyễn An", phone: request.PhoneNumber)));
    }

    public Task<ApiClientResult> ChangePasswordAsync(
        string accessToken,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        LastPasswordChange = request;
        return Task.FromResult(ApiClientResult.Success());
    }

    public Task<ApiClientResult> LogoutAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        LogoutCalls++;
        return Task.FromResult(ApiClientResult.Success());
    }

    private static AccountProfileResponse Profile(
        string email = "patient@mediqueue.test",
        string? fullName = "Nguyễn An",
        string? phone = "0901234567") => new(
            UserId,
            fullName,
            email,
            phone,
            "Patient",
            Guid.Parse("1dc8df16-4977-44dc-9a70-167fbf0b963b"),
            null,
            new DateOnly(1998, 5, 20));

    private static ApiProblem Unauthorized() => new(
        HttpStatusCode.Unauthorized,
        "Phiên không hợp lệ",
        "Vui lòng đăng nhập lại.",
        new Dictionary<string, string[]>());
}
