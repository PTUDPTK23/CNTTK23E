using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";

using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine($"=== KET NOI TOI CATALOG API TAI: {apiBaseUrl} ===\n");

Console.WriteLine("--- NAC 1: LAY DANH SACH SAN PHAM ---");
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");
Console.WriteLine("Danh sach san pham:");
foreach (var p in products ?? []) {
    Console.WriteLine($"  [{p.Id}] {p.Name} - {p.Price:N0} VND");
}

Console.WriteLine("\n--- NAC 2: TAO SAN PHAM MOI (POST) ---");
var createRequest = new CreateProductRequest(
    "Nhap mon REST API", "Sach tham khao", 120000);

var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);

if (createResponse.StatusCode == HttpStatusCode.Created) {
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"Da tao san pham moi: [{created?.Id}] {created?.Name}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");
} else {
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi: {error?.Code} - {error?.Message}");
    return;
}

Console.WriteLine("\n--- NAC 3: DOC CHI TIET SAN PHAM (GET /id) ---");
var detailResponse = await http.GetAsync("/api/products/1");
if (detailResponse.IsSuccessStatusCode) {
    var detail = await detailResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"Chi tiet san pham 1: {detail?.Name} - {detail?.Price:N0} VND");
} else {
    var error = await detailResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi: {error?.Code} - {error?.Message}");
}

Console.WriteLine("\n--- NAC 4: CO Y GUI DU LIEU SAI DE XEM LOI 400 ---");
var invalidResponse = await http.PostAsJsonAsync("/api/products", 
    new CreateProductRequest("", "", -1));

Console.WriteLine($"Thu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
var invalidError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"{invalidError?.Code}: {invalidError?.Message}");

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
