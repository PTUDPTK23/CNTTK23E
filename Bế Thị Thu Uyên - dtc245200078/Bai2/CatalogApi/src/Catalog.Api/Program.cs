
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu mau trong bo nho
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

// GET /
app.MapGet("/", () => Results.Json(new
{
    service = "Catalog.Api",
    status = "running"
}));

// GET /api/products
app.MapGet("/api/products", () => Results.Ok(products));

// GET /api/products/{id:int}
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

// POST /api/products : them san pham
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_NAME",
            "Ten san pham khong duoc de trong."
        ));
    }

    if (request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_PRICE",
            "Gia san pham phai lon hon 0."
        ));
    }

    int newId = products.Max(p => p.Id) + 1;

    var product = new ProductDto(
        newId,
        request.Name,
        request.Description ?? "",
        request.Price
    );

    products.Add(product);

    return Results.Created(
        $"/api/products/{product.Id}",
        product
    );
});
// PUT /api/products/{id} : cap nhat san pham
app.MapPut("/api/products/{id:int}", (int id, CreateProductRequest request) =>
{
    var index = products.FindIndex(p => p.Id == id);

    if (index == -1)
    {
        return Results.NotFound(new ApiError(
            "PRODUCT_NOT_FOUND",
            "Khong tim thay san pham."
        ));
    }

    var updatedProduct = new ProductDto(
        id,
        request.Name,
        request.Description ?? "",
        request.Price
    );

    products[index] = updatedProduct;

    return Results.Ok(updatedProduct);
});
// DELETE /api/products/{id} : xoa san pham
app.MapDelete("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    if (product is null)
    {
        return Results.NotFound(new ApiError(
            "PRODUCT_NOT_FOUND",
            "Khong tim thay san pham."
        ));
    }

    products.Remove(product);

    return Results.Ok(new
    {
        message = "Xoa san pham thanh cong.",
        product
    });
});
app.Run();

// DTO
record ProductDto(int Id, string Name, string Description, decimal Price);

record CreateProductRequest(string Name, string? Description, decimal Price);

record ApiError(string Code, string Message);
