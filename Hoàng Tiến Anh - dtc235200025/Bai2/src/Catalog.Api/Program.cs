var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Bước 3.2: Dữ liệu mẫu trong bộ nhớ
var products = new List<ProductDto>
{
    new(1, "Giáo trình Lập trình phân tán",
        "Giáo trình chính thức", 150000),

    new(2, "Tài liệu RESTful API",
        "Hướng dẫn chi tiết về API", 90000),

    new(3, "Bài tập thực hành .NET",
        "Bộ bài tập kèm lời giải", 120000)
};

// API kiểm tra trạng thái máy chủ
app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "running"
}));


// Bước 3.3: GET danh sách sản phẩm
app.MapGet("/api/products", () => Results.Ok(products));

 // Bước 4: GET chi tiết sản phẩm theo ID
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    return product is null
        ? Results.NotFound(new ApiError(
            "PRODUCT_NOT_FOUND",
            "Không tìm thấy sản phẩm."))
        : Results.Ok(product);
});
// POST /api/products : tạo mới từ body JSON
app.MapPost("/api/products", (CreateProductRequest request) => {
    // Kiểm tra tại biên hệ thống
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0) {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }
    
    // Phần tạo sản phẩm mới
    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price
    );

    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

// Kiểu dữ liệu sản phẩm
record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price
);
record CreateProductRequest(string Name, string? Description, decimal Price); // đầu vào

record ApiError(string Code, string Message);