var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Danh muc san pham duoc luu trong bo nho trong thoi gian API chay.
var products = new List<ProductDto>
{
    new(1, "Giao trinh lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

app.MapGet("/", () =>
    Results.Ok(new { server = "Catalog API", status = "running" }));

// GET /api/products?q=REST: lay danh sach, co the loc theo tu khoa query.
app.MapGet("/api/products", (string? q) =>
{
    IEnumerable<ProductDto> result = products.OrderBy(product => product.Id);

    if (!string.IsNullOrWhiteSpace(q))
    {
        result = result.Where(product =>
            product.Name.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase) ||
            (product.Description?.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase) ?? false));
    }

    return Results.Ok(result);
});

// GET /api/products/{id}: lay mot san pham theo id tren route.
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(item => item.Id == id);

    return product is null
        ? (IResult)Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Khong tim thay san pham."))
        : Results.Ok(product);
});

// POST /api/products: tao san pham tu JSON body.
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    var name = request.Name?.Trim();
    if (string.IsNullOrWhiteSpace(name))
    {
        return (IResult)Results.BadRequest(new ApiError("INVALID_PRODUCT", "Ten san pham khong duoc de trong."));
    }

    if (request.Price <= 0)
    {
        return (IResult)Results.BadRequest(new ApiError("INVALID_PRODUCT", "Gia san pham phai lon hon 0."));
    }

    var newId = products.Count == 0 ? 1 : products.Max(product => product.Id) + 1;
    var product = new ProductDto(newId, name, request.Description?.Trim(), request.Price);
    products.Add(product);

    return (IResult)Results.Created($"/api/products/{newId}", product);
});

app.Run();

record ProductDto(int Id, string Name, string? Description, decimal Price);
record CreateProductRequest(string? Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
