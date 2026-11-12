using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri)
    || (baseUri.Scheme != "http" && baseUri.Scheme != "https"))
{
    Console.Error.WriteLine("Dia chi API phai la URL http hoac https hop le.");
    Environment.ExitCode = 1;
    return;
}

using var http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(10) };
try
{
    // 1. Lay danh sach san pham.
    using var listResponse = await http.GetAsync("/api/products");
    if (!listResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(listResponse);
        return;
    }
    var products = await listResponse.Content.ReadFromJsonAsync<List<ProductDto>>();
    Console.WriteLine("Danh sach san pham:");
    foreach (var p in products ?? [])
        Console.WriteLine($"  [{p.Id}] {p.Name} - {p.Price:N0} VND");

    // 2. Tao san pham, kiem tra 201 truoc khi doc DTO.
    using var createResponse = await http.PostAsJsonAsync("/api/products",
        new CreateProductRequest("Nhap mon REST API", "Sach tham khao", 120000));
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"\nDa tao san pham moi: [{created?.Id}] {created?.Name}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");

    // 3. Doc chi tiet san pham 1 theo bai thuc hanh.
    using var detailResponse = await http.GetAsync("/api/products/1");
    if (!detailResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(detailResponse);
        return;
    }
    var detail = await detailResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"\nChi tiet san pham 1: {detail?.Name}");

    // 4. Co y gui sai de quan sat 400 va ApiError.
    using var invalidResponse = await http.PostAsJsonAsync("/api/products",
        new CreateProductRequest("", "", -1));
    Console.WriteLine($"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    if (invalidResponse.StatusCode != HttpStatusCode.BadRequest)
        Environment.ExitCode = 1;
    await PrintApiErrorAsync(invalidResponse, expected: true);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Loi ket noi den {apiBaseUrl}: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (TaskCanceledException)
{
    Console.Error.WriteLine("API khong phan hoi trong 10 giay.");
    Environment.ExitCode = 1;
}

static async Task PrintApiErrorAsync(HttpResponseMessage response, bool expected = false)
{
    if (response.IsSuccessStatusCode) return;
    if (!expected) Environment.ExitCode = 1;
    var body = await response.Content.ReadAsStringAsync();
    try
    {
        var error = JsonSerializer.Deserialize<ApiError>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (error is not null && !string.IsNullOrEmpty(error.Code))
        {
            Console.WriteLine($"{error.Code}: {error.Message}");
            return;
        }
    }
    catch (JsonException) { }
    Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}: khong co ApiError hop le.");
}

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
