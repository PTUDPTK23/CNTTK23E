
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Danh sách sản phẩm trong bộ nhớ
var products = new List<ProductDto>
{
    new ProductDto
    {
        Id = 1,
        Name = "Laptop",
        Description = "Laptop Dell",
        Price = 15000000
    },
    new ProductDto
    {
        Id = 2,
        Name = "Phone",
        Description = "Smartphone",
        Price = 10000000
    },
    new ProductDto
    {
        Id = 3,
        Name = "Keyboard",
        Description = "Ban phim co",
        Price = 500000
    }
};

// GET: Lấy danh sách sản phẩm
app.MapGet("/api/products", () =>
{
    return Results.Ok(products);
});

// GET: Lấy chi tiết sản phẩm
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    return product is null
        ? Results.NotFound(new ApiError(
            "PRODUCT_NOT_FOUND",
            "Khong tim thay san pham."))
        : Results.Ok(product);
});

// POST: Tạo sản phẩm mới
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    // Kiểm tra dữ liệu đầu vào
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    // Sinh ID tự động
    var product = new ProductDto
    {
        Id = products.Max(p => p.Id) + 1,
        Name = request.Name.Trim(),
        Description = request.Description?.Trim() ?? "",
        Price = request.Price
    };

    // Lưu sản phẩm vào danh sách
    products.Add(product);

    // Trả HTTP 201 Created kèm Location
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

// Model sản phẩm
record ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal Price { get; init; }
}

// Model nhận JSON
record CreateProductRequest(
    string? Name,
    decimal Price,
    string? Description
);

// Model thông báo lỗi
record ApiError(string Code, string Message);
