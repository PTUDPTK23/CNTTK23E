using System.Net;

namespace MediQueue.Web.Clients;

public sealed record ApiProblem(
    HttpStatusCode? StatusCode,
    string Title,
    string Detail,
    IReadOnlyDictionary<string, string[]> ValidationErrors,
    bool IsUnavailable = false)
{
    public static ApiProblem Unavailable() => new(
        null,
        "Không thể kết nối dịch vụ",
        "Dịch vụ MediQueue đang tạm gián đoạn. Vui lòng thử lại sau.",
        new Dictionary<string, string[]>(),
        true);
}

public sealed record ApiClientResult<T>(T? Value, ApiProblem? Problem)
{
    public bool Succeeded => Problem is null;

    public static ApiClientResult<T> Success(T value) => new(value, null);

    public static ApiClientResult<T> Failure(ApiProblem problem) => new(default, problem);
}

public sealed record ApiClientResult(ApiProblem? Problem)
{
    public bool Succeeded => Problem is null;

    public static ApiClientResult Success() => new((ApiProblem?)null);

    public static ApiClientResult Failure(ApiProblem problem) => new(problem);
}
