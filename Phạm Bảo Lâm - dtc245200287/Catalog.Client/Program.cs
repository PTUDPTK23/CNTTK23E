using System.Net;
using System.Net.Http.Json;

// Đọc URL từ dòng lệnh hoặc mặc định cổng 5100
var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine("==================================================");
Console.WriteLine($"[CLIENT] Đang kết nối tới Catalog API tại: {baseUrl}");
Console.WriteLine("==================================================\n");

// HOẠT ĐỘNG 1: GET toàn bộ danh sách sản phẩm
Console.WriteLine("--- 1. LẤY DANH SÁCH SẢN PHẨM (GET /api/products) ---");
try
{
    var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
    if (products != null)
    {
        foreach (var p in products)
        {
            Console.WriteLine($"  [ID {p.Id}] {p.Name} | Mô tả: {p.Description} | Giá: {p.Price:N0} VNĐ");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($" Lỗi khi gọi GET danh sách: {ex.Message}");
}

// HOẠT ĐỘNG 2: POST tạo mới sản phẩm hợp lệ
Console.WriteLine("\n--- 2. TẠO SẢN PHẨM MỚI HỢP LỆ (POST /api/products) ---");
var newReq = new CreateProductRequest("   Sách Thực Hành RESTful API ASP.NET   ", "Tài liệu học tập lập trình phân tán", 85000m);
var postResponse = await client.PostAsJsonAsync("/api/products", newReq);

if (postResponse.StatusCode == HttpStatusCode.Created)
{
    var createdProduct = await postResponse.Content.ReadFromJsonAsync<ProductDto>();
    var locationHeader = postResponse.Headers.Location?.ToString();
    Console.WriteLine($" Tạo sản phẩm thành công! (Mã HTTP: 201 Created)");
    Console.WriteLine($" Header Location: {locationHeader}");
    Console.WriteLine($" DTO nhận về: ID={createdProduct?.Id}, Name='{createdProduct?.Name}', Price={createdProduct?.Price:N0} VNĐ");
}
else
{
    Console.WriteLine($" Tạo sản phẩm thất bại. Mã HTTP: {postResponse.StatusCode}");
}

// HOẠT ĐỘNG 3: GET chi tiết sản phẩm ID = 1
Console.WriteLine("\n--- 3. LẤY CHI TIẾT SẢN PHẨM ID = 1 (GET /api/products/1) ---");
var detailProduct = await client.GetFromJsonAsync<ProductDto>("/api/products/1");
if (detailProduct != null)
{
    Console.WriteLine($" Tìm thấy sản phẩm: [ID {detailProduct.Id}] {detailProduct.Name} - {detailProduct.Price:N0} VNĐ");
}

// HOẠT ĐỘNG 4: POST cố ý gửi dữ liệu sai để nhận lỗi 400 Bad Request
Console.WriteLine("\n--- 4. GỬI DỮ LIỆU KHÔNG HỢP LỆ (POST /api/products - Name rỗng, Price <= 0) ---");
var invalidReq = new CreateProductRequest("", "Mô tả lỗi", -50000m);
var errorResponse = await client.PostAsJsonAsync("/api/products", invalidReq);

Console.WriteLine($" Trạng thái HTTP phản hồi: {(int)errorResponse.StatusCode} {errorResponse.StatusCode}");
if (errorResponse.StatusCode == HttpStatusCode.BadRequest)
{
    var apiError = await errorResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($" Chi tiết đối tượng ApiError giải tuần tự từ JSON:");
    Console.WriteLine($"   - Mã lỗi (Code): {apiError?.Code}");
    Console.WriteLine($"   - Thông điệp (Message): {apiError?.Message}");
}

// Định nghĩa các record DTO tương thích ở phía Client
public record ProductDto(int Id, string Name, string? Description, decimal Price);
public record CreateProductRequest(string Name, string? Description, decimal Price);
public record ApiError(string Code, string Message);