// VÙNG 1: KHỞI TẠO
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// VÙNG 2: DỮ LIỆU MẪU
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

// VÙNG 3: CÁC ENDPOINT
app.MapGet("/", () => Results.Ok(new {
    service = "Catalog.Api",
    status = "running"
}));

app.MapGet("/api/products", () => Results.Ok(products));

app.MapGet("/api/products/{id:int}", (int id) => {
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND",
                                       "Khong tim thay san pham."))
        : Results.Ok(product);
});

// POST /api/products : tạo mới từ body JSON (BƯỚC 5.2)
app.MapPost("/api/products", (CreateProductRequest request) => {
    // 1. Kiểm tra dữ liệu hợp lệ ở biên hệ thống
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0) {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    // 2. Máy chủ tự sinh Id, làm sạch dữ liệu và tạo DTO
    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price);

    // 3. Lưu vào danh sách và trả về 201 Created kèm Header Location
    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

// VÙNG 4: CHẠY
app.Run();

// VÙNG 5: KHAI BÁO KIỂU (BƯỚC 5.1)
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);