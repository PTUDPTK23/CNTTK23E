
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// VUNG 2: Du lieu mau trong bo nho
var products = new List<ProductDto>
{
    new (1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new (2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new (3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000),
};

// VUNG 3: GET / - Kiem tra may chu
app.MapGet("/", () => Results.Ok(new
{
    server = "Catalog.Api",
    status = "running",
}));

// GET /api/products - Lay danh sach san pham
app.MapGet("/api/products", () => Results.Ok(products));

// GET /api/products/{id} - Lay chi tiet san pham
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    return product is null
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham"))
        : Results.Ok(product);
});

// POST /api/products - Tao san pham moi
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    // Kiem tra du lieu dau vao
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0"
        ));
    }

    // Tao san pham moi
    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price
    );

    // Them san pham vao danh sach
    products.Add(product);

    // Tra ve 201 Created
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

// VUNG 5: DTO - Hinh dang du lieu di qua mang

// Du lieu dau ra
record ProductDto(int Id, string Name, string Description, decimal Price);

// Du lieu dau vao
record CreateProductRequest(string Name, string? Description, decimal Price);

// Doi tuong thong bao loi
record ApiError(string Code, string Message);
