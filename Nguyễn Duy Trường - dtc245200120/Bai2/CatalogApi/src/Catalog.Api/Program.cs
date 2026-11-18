var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var products = new List<ProductDto>
{
    new(1, "Giao trinh lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh > NET", "Bo bai tap kem loi giai", 120000),
};
app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "running"
}));

app.MapGet("/api/products", () => Results.Ok(products));

 // GET /api/products/{id:int} : Chi tiet mot san pham
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    return product is null
        ? Results.NotFound(new ApiError(
            "PRODUCT_NOT_FOUND",
            "Khong tim thay san pham."))
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

    // Phan tao san pham: slide sau...
        // Tao san pham moi, tu dong sinh ID
    var product = new ProductDto(
        Id: products.Max(p => p.Id) + 1,
        Name: request.Name.Trim(),
        Description: request.Description?.Trim() ?? "",
        Price: request.Price
    );

    // Them san pham vao danh sach
    products.Add(product);

    // Tra ve HTTP 201 Created
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();
record  ProductDto(int Id, string Name,string Description, decimal Price);
record CreateProductRequest(string Name,string? Description, decimal Price);
record ApiError(String Code, string Message);