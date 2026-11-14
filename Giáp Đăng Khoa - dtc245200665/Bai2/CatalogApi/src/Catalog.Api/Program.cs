var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var products = new List<ProductDto>
{
    new(1,"Gao đỏ", "Nhân vật trong siêu nhân GAO", 30000),
    new(2,"Doreamon", "Nhân vật trong phim hoạt hình", 30000),
    new(3,"HP 3060", "Máy tính HP", 30000),
};


app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "Running"
}));

app.MapGet("/api/products", () => Results.Ok(products));

app.MapGet("/api/products{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.id == id);
    return product is null 
        ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm")) 
        : Results.Ok(product);
});

app.MapPost("/api/products", (CreateProductRequest request) =>
{
    if(string.IsNullOrWhiteSpace(request.name) || request.price <= 0)
    {
        return Results.BadRequest( new ApiError(
            "INVALID_PRODUCT", "Tên Sản Phẩm Không Để Trống Và Giá Phải Lớn Hơn 0"
        ));
    }
    var product = new ProductDto
    (
        id: products.Max(p => p.id) +1,
        name: request.name.Trim(),
        description: request.description.Trim() ?? "",
        price: request.price
    );
    products.Add(product);
    return Results.Created($"/api/products/{product.id}", product);
});
app.Run();

record ProductDto(int id, string name, string description, decimal price);
record ApiError(string code, string message);
record CreateProductRequest(string name, string description, decimal price);