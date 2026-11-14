
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "running"
}));

var products = new Dictionary<int, ProductDto>
{
    [1] = new ProductDto(1, "Class Laptop", "Laptop Dell", 15000000),
    [2] = new ProductDto(2, "Class Phone", "iPhone 15", 18000000),
    [3] = new ProductDto(3, "Class Tablet", "Samsung Galaxy Tab", 12000000)
};

app.MapGet("/api/products", () =>
{
    return Results.Ok(products.Values);
});

app.MapGet("/api/products/{id:int}", (int id) =>
{
    if (!products.TryGetValue(id, out var product))
    {
        return Results.NotFound(
            new ApiError("PRODUCT_NOT_FOUND", "Product not found")
        );
    }

    return Results.Ok(product);
});

app.MapPost("/api/products", (CreateProductRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(
            new ApiError(
                "INVALID_PRODUCT",
                "Ten san pham khong duoc de trong va gia phai lon hon 0."
            )
        );
    }

    var product = new ProductDto(
        products.Keys.DefaultIfEmpty(0).Max() + 1,
        request.Name.Trim(),
        request.Description?.Trim() ?? "",
        request.Price
    );

    products[product.Id] = product;

    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

record ProductDto(int Id, string Name, string? Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
