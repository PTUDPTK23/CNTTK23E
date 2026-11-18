using System.Net;
using System.Net.Http.Json;

using var http = new HttpClient { BaseAddress = new Uri(args.FirstOrDefault() ?? "http://localhost:5100"), Timeout = TimeSpan.FromSeconds(10) };
try
{
    using var listResponse = await http.GetAsync("/api/products");
    if (!listResponse.IsSuccessStatusCode) { await PrintErrorAsync(listResponse); return; }
    var products = await listResponse.Content.ReadFromJsonAsync<List<ProductDto>>();
    Console.WriteLine("1. Danh sach san pham:");
    foreach (var p in products ?? []) Console.WriteLine($"[{p.Id}] {p.Name} - {p.Price:N0} VND");
    using var response = await http.PostAsJsonAsync("/api/products", new CreateProductRequest("Nhap mon REST API", "Sach tham khao", 120000));
    if (response.StatusCode != HttpStatusCode.Created) { await PrintErrorAsync(response); return; }
    var created = await response.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"2. Da tao: [{created?.Id}] {created?.Name}; Location: {response.Headers.Location}");
    if (response.Headers.Location is not { } location) throw new InvalidOperationException("API khong tra Location.");
    using var detailResponse = await http.GetAsync(location);
    if (!detailResponse.IsSuccessStatusCode) { await PrintErrorAsync(detailResponse); return; }
    var detail = await detailResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"3. Doc lai: [{detail?.Id}] {detail?.Name}");
    using var invalidResponse = await http.PostAsJsonAsync("/api/products", new CreateProductRequest("", "", -1));
    Console.WriteLine("4. Thu gui du lieu sai:");
    await PrintErrorAsync(invalidResponse);
}
catch (HttpRequestException ex) { Console.WriteLine($"Loi ket noi: {ex.Message}"); Environment.ExitCode = 1; }
catch (TaskCanceledException) { Console.WriteLine("Het thoi gian cho API."); Environment.ExitCode = 1; }

static async Task PrintErrorAsync(HttpResponseMessage response)
{
    Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}");
    var body = await response.Content.ReadAsStringAsync();
    try
    {
        var error = System.Text.Json.JsonSerializer.Deserialize<ApiError>(body, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Console.WriteLine($"{error?.Code}: {error?.Message}");
    }
    catch (System.Text.Json.JsonException) { Console.WriteLine(string.IsNullOrWhiteSpace(body) ? "Phan hoi khong co body JSON." : body); }
}

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
