using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Khởi tạo dữ liệu mảng In-Memory
var products = new List<ProductDto>
{
    new(1, "Laptop Dell XPS 13", "Laptop mỏng nhẹ cao cấp", 25000000m),
    new(2, "Bàn phím cơ Keychron K2", "Bàn phím cơ không dây Bluetooth", 1800000m),
    new(3, "Chuột Logitech MX Master 3S", "Chuột không dây chuyên đồ họa", 2200000m)
};

// 1. GET / - Kiểm tra trạng thái máy chủ
app.MapGet("/", () => Results.Ok(new { service = "Catalog API", status = "Healthy" }));

// 2. GET /api/products - Danh sách sản phẩm (hỗ trợ lọc theo query string 'keyword')
app.MapGet("/api/products", ([FromQuery] string? keyword) =>
{
    if (string.IsNullOrWhiteSpace(keyword))
    {
        return Results.Ok(products);
    }
    var filtered = products
        .Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                   (p.Description != null && p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        .ToList();
    return Results.Ok(filtered);
});

// 3. GET /api/products/{id:int} - Chi tiết sản phẩm theo ID
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null 
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", $"Không tìm thấy sản phẩm có mã ID = {id}."))
        : Results.Ok(product);
});

// 4. POST /api/products - Tạo sản phẩm mới
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError("INVALID_PRODUCT", "Tên sản phẩm không được rỗng và giá phải lớn hơn 0."));
    }
    var cleanName = request.Name.Trim();
    var cleanDescription = request.Description?.Trim();
    int nextId = products.Count > 0 ? products.Max(p => p.Id) + 1 : 1;
    var newProduct = new ProductDto(nextId, cleanName, cleanDescription, request.Price);
    products.Add(newProduct);
    return Results.Created($"/api/products/{newProduct.Id}", newProduct);
});

app.Run();

public record ProductDto(int Id, string Name, string? Description, decimal Price);
public record CreateProductRequest(string Name, string? Description, decimal Price);
public record ApiError(string Code, string Message);