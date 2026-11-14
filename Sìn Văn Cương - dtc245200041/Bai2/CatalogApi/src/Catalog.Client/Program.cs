using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");
Console.WriteLine("Danh sách sản phẩm: ");
foreach(var p in products ?? [])
{
    Console.WriteLine($" [{p.id}] {p.name} - {p.price:N0} VND");
}

var createRequest = new CreateProductRequest("Nhap mon REST API", "Sách tham khảo", 1200);

var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);
if(createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"\nĐã tạo sản phẩm mới: [{created?.id}] {created?.name}");
    Console.WriteLine($"\nĐọc lại tại: {createResponse.Headers.Location}");
} else
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"\nLỗi: {error?.code} - {error?.message}");
    return;
}

var detail = await http.GetFromJsonAsync<ProductDto>("/api/product/1");
Console.WriteLine($"\nChi tiết sản phẩm 1: {detail?.name}");

var invalidResponse = await http.PostAsJsonAsync("/api/products", new CreateProductRequest("","",1));

Console.WriteLine($"\nThử gửi dữ liệu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
var invaldError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"{invaldError?.code} - {invaldError?.message}");

record ProductDto(int id, string name, string description, decimal price);
record ApiError(string code, string message);
record CreateProductRequest(string name, string description, decimal price);