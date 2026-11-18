var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
// Dữ liệu mẫu trong bộ nhớ
var products = new List<ProductDto>
{
    new(1, "Giao trinh lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh > NET", "Bo bai tap kem loi giai", 120000),
};

app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "running",

}));
app.MapGet("/api/products", () => Results.Ok(products));
// GET /api/products/{id:int}: chi tiết một sản phẩm
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    if (product is null)
    {
        return Results.NotFound(
            new ApiError("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.")
        );
    }


    return Results.Ok(product);
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

    var newProduct = new ProductDto(
        products.Max(p => p.Id) + 1,
        request.Name,
        request.Description ?? string.Empty,
        request.Price
    );
    products.Add(newProduct);
    return Results.Created($"/api/products/{newProduct.Id}", newProduct);
});


app.Run();
// DTO: hình dạng dữ liệu đi qua mạng
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductDto(string Name, string? Description, decimal Price);

record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
