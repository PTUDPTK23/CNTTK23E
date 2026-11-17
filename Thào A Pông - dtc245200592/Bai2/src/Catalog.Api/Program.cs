
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Du lieu mau trong bo nho
var products = new List<ProductDto>
{
    new(1, "Giao trinh lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap loi giai", 120000)
};

// GET /
app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "running"
}));

// GET /api/products: Danh sach san pham
app.MapGet("/api/products", () => Results.Ok(products));

// GET /api/products/{id:int}: Chi tiet san pham
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    return product is null
        ? Results.NotFound(new ApiError(
            "PRODUCT_NOT_FOUND",
            "Khong tim thay san pham."
        ))
        : Results.Ok(product);
});

// POST /api/products: Them san pham
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_REQUEST",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."
        ));
    }

    // Tao san pham moi
    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price
    );

    // Luu san pham vao danh sach
    products.Add(product);

    // Tra HTTP 201 Created kem Location
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

// DTO
record ProductDto(int Id, string Name, string Description, decimal Price);

record ApiError(string Code, string Message);

record CreateProductRequest(string Name, string? Description, decimal Price);
