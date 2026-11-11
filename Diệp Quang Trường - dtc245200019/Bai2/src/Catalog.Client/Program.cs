
using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// GET: Lay danh sach san pham
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");

Console.WriteLine("Danh sach san pham:");

foreach (var p in products ?? [])
{
    Console.WriteLine($" [{p.Id}] {p.Name} - {p.Price:N0} VND");
}

// POST: Tao san pham moi
var createRequest = new CreateProductDto(
    "Nhap mon REST API",
    "Sach tham khao",
    120000
);

var createResponse = await http.PostAsJsonAsync(
    "/api/products",
    createRequest
);

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

    Console.WriteLine($"\nDa tao san pham moi: [{created?.Id}] {created?.Name}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");
}
else
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();

    Console.WriteLine($"\nLoi: {error?.Code} - {error?.Message}");
    return;
}

// GET: Chi tiet san pham ID = 1
var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");

Console.WriteLine($"\nChi tiet san pham 1: {detail?.Name}");

// POST: Thu gui du lieu sai
var invalidResponse = await http.PostAsJsonAsync(
    "/api/products",
    new CreateProductDto("", "", -1)
);

Console.WriteLine($"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode}");

var invalidError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();

Console.WriteLine($"{invalidError?.Code}: {invalidError?.Message}");

// GET: Doc lai danh sach sau khi tao
var updatedProducts = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");

Console.WriteLine("\nDanh sach san pham sau khi them:");

foreach (var p in updatedProducts ?? [])
{
    Console.WriteLine($" [{p.Id}] {p.Name} - {p.Price:N0} VND");
}

// Cac record cuoi file
record ProductDto(int Id, string Name, string Description, decimal Price);

record CreateProductDto(string Name, string? Description, decimal Price);

record ApiError(string Code, string Message);
