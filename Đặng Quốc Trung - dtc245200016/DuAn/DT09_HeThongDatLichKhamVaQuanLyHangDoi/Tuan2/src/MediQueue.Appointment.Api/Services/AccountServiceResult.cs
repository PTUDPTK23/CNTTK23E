namespace MediQueue.Appointment.Api.Services;

public sealed record AccountServiceError(
    int StatusCode,
    string Title,
    string Detail,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public sealed record AccountServiceResult<T>(T? Value, AccountServiceError? Error)
{
    public bool Succeeded => Error is null;

    public static AccountServiceResult<T> Success(T value) => new(value, null);

    public static AccountServiceResult<T> Failure(AccountServiceError error) => new(default, error);
}

public sealed record AccountServiceResult(AccountServiceError? Error)
{
    public bool Succeeded => Error is null;

    public static AccountServiceResult Success() => new((AccountServiceError?)null);

    public static AccountServiceResult Failure(AccountServiceError error) => new(error);
}
