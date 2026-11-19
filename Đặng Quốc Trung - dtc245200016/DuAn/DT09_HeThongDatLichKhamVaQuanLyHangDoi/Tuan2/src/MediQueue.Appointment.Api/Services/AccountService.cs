using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediQueue.Appointment.Api.Security;
using MediQueue.Contracts.Authentication;
using MediQueue.Domain.Common;
using MediQueue.Domain.Patients;
using MediQueue.Domain.Security;
using MediQueue.Domain.Time;
using MediQueue.Infrastructure.Identity;
using MediQueue.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediQueue.Appointment.Api.Services;

public sealed class AccountService(
    MediQueueDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    TimeProvider timeProvider) : IAccountService
{
    public async Task<AccountServiceResult<AccountProfileResponse>> RegisterPatientAsync(
        RegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        string email = request.Email.Trim();
        string fullName = request.FullName.Trim();
        string? phoneNumber = NormalizeOptional(request.PhoneNumber);

        if (request.DateOfBirth is { } dateOfBirth &&
            dateOfBirth > BusinessTime.GetVietnamServiceDate(timeProvider.GetUtcNow()))
        {
            return Validation<AccountProfileResponse>(nameof(request.DateOfBirth),
                "Ngày sinh không thể nằm trong tương lai.");
        }

        if (!await dbContext.Roles.AnyAsync(
                role => role.Name == SystemRoles.Patient,
                cancellationToken))
        {
            return Failure<AccountProfileResponse>(500,
                "Hệ thống chưa sẵn sàng",
                "Role hệ thống chưa được khởi tạo.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                PhoneNumber = phoneNumber,
                IsActive = true,
                CreatedAtUtc = timeProvider.GetUtcNow()
            };

            IdentityResult createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return IdentityFailure<AccountProfileResponse>(createResult);
            }

            IdentityResult roleResult = await userManager.AddToRoleAsync(user, SystemRoles.Patient);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return IdentityFailure<AccountProfileResponse>(roleResult);
            }

            var patient = new Patient(Guid.NewGuid(), user.Id, fullName, request.DateOfBirth);
            patient.UpdateProfile(fullName, phoneNumber);
            dbContext.Patients.Add(patient);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return AccountServiceResult<AccountProfileResponse>.Success(
                ToProfile(user, SystemRoles.Patient, patient, null));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure<AccountProfileResponse>(409,
                "Không thể tạo tài khoản",
                "Email đã được sử dụng hoặc dữ liệu xung đột.");
        }
    }

    public async Task<AccountServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        string email = request.Email.Trim();
        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            PasswordHasher<ApplicationUser> hasher = new();
            _ = hasher.VerifyHashedPassword(
                new ApplicationUser(),
                hasher.HashPassword(new ApplicationUser(), "MediQueue-Dummy-Password"),
                request.Password);
            return InvalidCredentials<LoginResponse>();
        }

        if (await userManager.IsLockedOutAsync(user) || !user.IsActive)
        {
            return InvalidCredentials<LoginResponse>();
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return InvalidCredentials<LoginResponse>();
        }

        await userManager.ResetAccessFailedCountAsync(user);

        IList<string> roles = await userManager.GetRolesAsync(user);
        if (roles.Count != 1 || !SystemRoles.All.Contains(roles[0], StringComparer.Ordinal))
        {
            return Failure<LoginResponse>(403,
                "Tài khoản không có quyền truy cập",
                "Tài khoản chưa được gán đúng một Role hợp lệ.");
        }

        string role = roles[0];
        AccountProfileResponse profile = await BuildProfileAsync(user, role, cancellationToken);
        string securityStamp = await userManager.GetSecurityStampAsync(user);
        IssuedAccessToken token = jwtTokenService.IssueToken(user.Id, role, securityStamp);

        return AccountServiceResult<LoginResponse>.Success(new LoginResponse(
            token.Value,
            "Bearer",
            token.ExpiresAtUtc,
            user.Id,
            profile.FullName,
            user.Email!,
            role));
    }

    public async Task<AccountServiceResult> LogoutAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await GetCurrentUserAsync(principal);
        if (user is null)
        {
            return AccountServiceResult.Failure(Unauthorized());
        }

        IdentityResult result = await userManager.UpdateSecurityStampAsync(user);
        return result.Succeeded
            ? AccountServiceResult.Success()
            : AccountServiceResult.Failure(IdentityError(result));
    }

    public async Task<AccountServiceResult<AccountProfileResponse>> GetProfileAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await GetCurrentUserAsync(principal);
        string? role = principal.FindFirstValue(JwtOptions.RoleClaim);
        if (user is null || role is null)
        {
            return AccountServiceResult<AccountProfileResponse>.Failure(Unauthorized());
        }

        return AccountServiceResult<AccountProfileResponse>.Success(
            await BuildProfileAsync(user, role, cancellationToken));
    }

    public async Task<AccountServiceResult<AccountProfileResponse>> UpdateProfileAsync(
        ClaimsPrincipal principal,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await GetCurrentUserAsync(principal);
        string? role = principal.FindFirstValue(JwtOptions.RoleClaim);
        if (user is null || role is null)
        {
            return AccountServiceResult<AccountProfileResponse>.Failure(Unauthorized());
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
            IdentityResult updateUserResult = await userManager.UpdateAsync(user);
            if (!updateUserResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return AccountServiceResult<AccountProfileResponse>.Failure(IdentityError(updateUserResult));
            }

            if (role == SystemRoles.Patient)
            {
                Patient? patient = await dbContext.Patients.SingleOrDefaultAsync(
                    item => item.UserId == user.Id,
                    cancellationToken);
                if (patient is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure<AccountProfileResponse>(409, "Hồ sơ không hợp lệ",
                        "Tài khoản Patient chưa có hồ sơ bệnh nhân.");
                }

                patient.UpdateProfile(request.FullName ?? patient.FullName, user.PhoneNumber);
            }
            else if (role == SystemRoles.Doctor)
            {
                var doctor = await dbContext.Doctors.SingleOrDefaultAsync(
                    item => item.UserId == user.Id,
                    cancellationToken);
                if (doctor is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure<AccountProfileResponse>(409, "Hồ sơ không hợp lệ",
                        "Tài khoản Doctor chưa có hồ sơ bác sĩ.");
                }

                doctor.UpdateDisplayName(request.FullName ?? doctor.DisplayName);
            }
            else if (request.FullName is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Validation<AccountProfileResponse>(nameof(request.FullName),
                    "Phiên bản hiện tại chưa lưu họ tên cho Receptionist/Admin.");
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AccountServiceResult<AccountProfileResponse>.Success(
                await BuildProfileAsync(user, role, cancellationToken));
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure<AccountProfileResponse>(409, "Xung đột dữ liệu",
                "Hồ sơ đã được thay đổi bởi một yêu cầu khác.");
        }
        catch (DomainRuleException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Validation<AccountProfileResponse>(nameof(request.FullName), exception.Message);
        }
    }

    public async Task<AccountServiceResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await GetCurrentUserAsync(principal);
        if (user is null)
        {
            return AccountServiceResult.Failure(Unauthorized());
        }

        IdentityResult result = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);
        return result.Succeeded
            ? AccountServiceResult.Success()
            : AccountServiceResult.Failure(IdentityError(result));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        string? subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(subject, out Guid userId)
            ? await userManager.FindByIdAsync(userId.ToString())
            : null;
    }

    private async Task<AccountProfileResponse> BuildProfileAsync(
        ApplicationUser user,
        string role,
        CancellationToken cancellationToken)
    {
        Patient? patient = role == SystemRoles.Patient
            ? await dbContext.Patients.AsNoTracking().SingleOrDefaultAsync(
                item => item.UserId == user.Id, cancellationToken)
            : null;
        var doctor = role == SystemRoles.Doctor
            ? await dbContext.Doctors.AsNoTracking().SingleOrDefaultAsync(
                item => item.UserId == user.Id, cancellationToken)
            : null;

        return ToProfile(user, role, patient, doctor);
    }

    private static AccountProfileResponse ToProfile(
        ApplicationUser user,
        string role,
        Patient? patient,
        MediQueue.Domain.Doctors.Doctor? doctor) => new(
            user.Id,
            patient?.FullName ?? doctor?.DisplayName,
            user.Email!,
            user.PhoneNumber,
            role,
            patient?.Id,
            doctor?.Id,
            patient?.DateOfBirth);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AccountServiceResult<T> InvalidCredentials<T>() =>
        Failure<T>(401, "Đăng nhập thất bại", "Email hoặc mật khẩu không đúng.");

    private static AccountServiceError Unauthorized() =>
        new(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");

    private static AccountServiceResult<T> Validation<T>(string field, string message) =>
        AccountServiceResult<T>.Failure(new AccountServiceError(
            400,
            "Dữ liệu không hợp lệ",
            "Yêu cầu có dữ liệu không hợp lệ.",
            new Dictionary<string, string[]> { [field] = [message] }));

    private static AccountServiceResult<T> Failure<T>(int status, string title, string detail) =>
        AccountServiceResult<T>.Failure(new AccountServiceError(status, title, detail));

    private static AccountServiceResult<T> IdentityFailure<T>(IdentityResult result) =>
        AccountServiceResult<T>.Failure(IdentityError(result));

    private static AccountServiceError IdentityError(IdentityResult result)
    {
        bool duplicate = result.Errors.Any(error =>
            error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
        bool password = result.Errors.Any(error =>
            error.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            return new AccountServiceError(409, "Dữ liệu đã tồn tại",
                "Email đã được sử dụng.");
        }

        if (password)
        {
            return new AccountServiceError(400, "Mật khẩu không hợp lệ",
                "Mật khẩu không đáp ứng chính sách bảo mật.",
                new Dictionary<string, string[]> { ["Password"] = ["Mật khẩu phải có ít nhất 12 ký tự và 3 ký tự khác nhau."] });
        }

        return new AccountServiceError(400, "Không thể cập nhật tài khoản",
            "Yêu cầu tài khoản không hợp lệ.");
    }
}
