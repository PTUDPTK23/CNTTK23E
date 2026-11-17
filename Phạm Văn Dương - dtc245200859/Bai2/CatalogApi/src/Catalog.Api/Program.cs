var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu trong bộ nhớ
var products = new List<ProductDto>
{
    new(1, "Giáo trình lập trình phần mềm", "Giáo trình chính thức", 150000),
    new(2, "Tài liệu RESTful API", "Hướng dẫn thiết kế API", 90000),
    new(3, "Bài tập thực hành .NET", "Bộ bài tập kèm lời giải", 120000)
};

app.MapGet("/", () => Results.Ok( new {
    Service = "Catalog.Api",
    status = "Running"
}));

// GET /api/products : danh sách sản phẩm
app.MapGet("/api/products", () => Results.Ok(products));

// GET /api/products/{id:int} : chi tiết một sản phẩm
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham."))
        : Results.Ok(product);
});

// POST /api/products : tao moi tu body JSON
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    // Kiem tra toi thieu, khong du thong tin
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_PRODUCT",
            "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price);

    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

record ProductDto(int Id, string Name, string Description, decimal Price);

record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
