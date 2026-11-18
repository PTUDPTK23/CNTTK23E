
using System.Net;
using System.Net.Http.Json;

var apiUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5100";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiUrl),
    Timeout = TimeSpan.FromSeconds(10)
};

Console.WriteLine("=== CATALOG CLIENT ===");
Console.WriteLine($"Server: {apiUrl}");

try
{
    // NAC 1: GET danh sach san pham
    var response = await http.GetAsync("/api/products");

    Console.WriteLine(
        $"HTTP: {(int)response.StatusCode}");

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine(
            await response.Content.ReadAsStringAsync());
        return;
    }

    var products = await response.Content
        .ReadFromJsonAsync<List<ProductDto>>();

    Console.WriteLine("\nDanh sach san pham:");

    foreach (var p in products ?? new List<ProductDto>())
    {
        Console.WriteLine(
            $"[{p.Id}] {p.Name} - {p.Price:N0} VND");
    }

    // NAC 2: POST tao san pham moi
    var createRequest = new CreateProductRequest(
        "Nhap mon REST API",
        "Sach tham khao",
        120000
    );

    using var createResponse = await http.PostAsJsonAsync(
        "/api/products",
        createRequest
    );

    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var created = await createResponse.Content
            .ReadFromJsonAsync<ProductDto>();

        Console.WriteLine(
            $"\nDa tao san pham moi: [{created?.Id}] {created?.Name}");

        Console.WriteLine(
            $"Doc lai tai: {createResponse.Headers.Location}");
    }
    else
    {
        var error = await createResponse.Content
            .ReadFromJsonAsync<ApiError>();

        Console.WriteLine(
            $"\nLoi: {error?.Code} - {error?.Message}");
        return;
    }

    // NAC 3: GET chi tiet san pham
    var detail = await http
        .GetFromJsonAsync<ProductDto>("/api/products/1");

    Console.WriteLine(
        $"\nChi tiet san pham 1: {detail?.Name}");

    // NAC 4: POST du lieu sai, nhan HTTP 400
    using var invalidResponse = await http.PostAsJsonAsync(
        "/api/products",
        new CreateProductRequest("", "", -1)
    );

    Console.WriteLine(
        $"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");

    var invalidError = await invalidResponse.Content
        .ReadFromJsonAsync<ApiError>();

    Console.WriteLine(
        $"{invalidError?.Code}: {invalidError?.Message}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi ket noi: {ex.Message}");
}
catch (TaskCanceledException)
{
    Console.WriteLine("Loi: Het thoi gian cho API.");
}

// Cac DTO cua may khach
record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price
);

record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price
);

record ApiError(string Code, string Message);
