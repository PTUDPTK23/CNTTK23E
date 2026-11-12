using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// 1. Lấy danh sách sản phẩm
var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");

Console.WriteLine("Danh sach san pham:");
foreach (var product in products ?? [])
{
    Console.WriteLine($"  [{product.Id}] {product.Name} - {product.Price:N0} VND");
}

// 2. Tạo sản phẩm và đọc mã trạng thái trước khi đọc JSON
var createRequest = new CreateProductRequest(
    "Nhap mon REST API", "Sach tham khao", 120000);

using var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
    Console.WriteLine($"\nDa tao san pham moi: [{created?.Id}] {created?.Name}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");
}
else
{
    Console.WriteLine($"\nTao san pham that bai: {(int)createResponse.StatusCode} {createResponse.StatusCode}");
    Console.WriteLine(await createResponse.Content.ReadAsStringAsync());
    return;
}

// 3. Xem chi tiết sản phẩm số 1
var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");
Console.WriteLine($"\nChi tiet san pham 1: {detail?.Name}");

// 4. Gửi dữ liệu sai để kiểm tra phản hồi 400
using var invalidResponse = await http.PostAsJsonAsync(
    "/api/products", new CreateProductRequest("", "", -1));

Console.WriteLine($"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");

if (invalidResponse.StatusCode == HttpStatusCode.BadRequest)
{
    var error = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);