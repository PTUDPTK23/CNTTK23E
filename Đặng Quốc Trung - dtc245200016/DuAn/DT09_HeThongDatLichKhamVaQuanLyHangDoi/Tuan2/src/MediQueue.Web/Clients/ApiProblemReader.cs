using System.Net;
using System.Text.Json;

namespace MediQueue.Web.Clients;

internal static class ApiProblemReader
{
    public static async Task<ApiProblem> ReadAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            string title = GetString(root, "title") ?? "Yêu cầu chưa được xử lý";
            string detail = GetString(root, "detail") ?? "Vui lòng kiểm tra thông tin và thử lại.";
            var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("errors", out JsonElement validationErrors) &&
                validationErrors.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in validationErrors.EnumerateObject())
                {
                    errors[property.Name] = property.Value.ValueKind == JsonValueKind.Array
                        ? property.Value.EnumerateArray()
                            .Select(item => item.GetString())
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .Cast<string>()
                            .ToArray()
                        : [];
                }
            }

            return new ApiProblem(response.StatusCode, title, detail, errors);
        }
        catch (JsonException)
        {
            return new ApiProblem(
                response.StatusCode,
                "Yêu cầu chưa được xử lý",
                "Dịch vụ không thể xử lý yêu cầu vào lúc này.",
                new Dictionary<string, string[]>());
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
