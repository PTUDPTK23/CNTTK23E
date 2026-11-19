namespace MediQueue.Infrastructure.Identity;

public interface IIdentityBootstrapper
{
    Task<IdentityBootstrapResult> InitializeRolesAsync(CancellationToken cancellationToken = default);

    Task<IdentityBootstrapResult> CreateInitialAdminAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record IdentityBootstrapResult(
    bool Succeeded,
    bool Changed,
    string Message,
    IReadOnlyList<string> CreatedRoles)
{
    public static IdentityBootstrapResult Success(
        bool changed,
        string message,
        IReadOnlyList<string>? createdRoles = null) =>
        new(true, changed, message, createdRoles ?? []);

    public static IdentityBootstrapResult Failure(string message) =>
        new(false, false, message, []);
}
