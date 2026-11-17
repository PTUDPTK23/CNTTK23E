var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Danh sách sản phẩm mẫu
var products = new List<ProductDto>
{
    new(1, "Laptop", "Laptop Dell Inspiron", 18000000),
    new(2, "Mouse", "Logitech M185", 250000),
    new(3, "Keyboard", "AKKO 3087", 1200000)
};

// GET /
app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        service = "Catalog.Api",
        status = "running"
    });
});

// GET tất cả
app.MapGet("/api/products", () =>
{
    return Results.Ok(products);
});

// GET theo ID
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    if (product is null)
        return Results.NotFound();

    return Results.Ok(product);
});

// POST
app.MapPost("/api/products", (ProductDto product) =>
{
    products.Add(product);

    return Results.Created(
        $"/api/products/{product.Id}",
        product);
});

// PUT cập nhật sản phẩm
app.MapPut("/api/products/{id:int}", (int id, ProductDto updatedProduct) =>
{
    var index = products.FindIndex(p => p.Id == id);

    if (index == -1)
        return Results.NotFound();

    products[index] = updatedProduct;

    return Results.NoContent();
});

// DELETE sản phẩm
app.MapDelete("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    if (product is null)
        return Results.NotFound();

    products.Remove(product);

    return Results.NoContent();
});

app.Run();

record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price
);