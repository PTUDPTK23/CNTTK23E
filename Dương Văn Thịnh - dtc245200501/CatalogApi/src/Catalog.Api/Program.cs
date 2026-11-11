
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu san pham
var products = new List<ProductDto>
{
    new(1, "Giao trinh Lap trinh phan tan", "Giao trinh chinh thuc", 150000),
    new(2, "Tai lieu RESTful API", "Huong dan thiet ke API", 90000),
    new(3, "Bai tap thuc hanh .NET", "Bo bai tap kem loi giai", 120000)
};

// GET /
app.MapGet("/", () => Results.Ok(new
{
    service = "Catalog.Api",
    status = "running"
}));

// GET danh sach san pham
app.MapGet("/api/products", () =>
{
    return Results.Ok(products);
});

// GET san pham theo ID
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);

    if (product == null)
    {
        return Results.NotFound(new ApiError(
            "PRODUCT_NOT_FOUND",
            "Khong tim thay san pham."
        ));
    }

    return Results.Ok(product);
});

// POST them san pham
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    // Kiem tra ten khong duoc de trong
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_NAME",
            "Ten san pham khong duoc de trong."
        ));
    }

    // Kiem tra ten toi da 100 ky tu
    if (request.Name.Length > 100)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_NAME",
            "Ten san pham khong duoc vuot qua 100 ky tu."
        ));
    }

    // Kiem tra gia phai lon hon 0
    if (request.Price <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_PRICE",
            "Gia san pham phai lon hon 0."
        ));
    }

    // Tao ID moi
    var newId = products.Count == 0
        ? 1
        : products.Max(p => p.Id) + 1;

    // Tao san pham moi
    var product = new ProductDto(
        newId,
        request.Name,
        request.Description,
        request.Price
    );

    products.Add(product);

    // Tra ve 201 Created
    return Results.Created(
        $"/api/products/{product.Id}",
        product
    );
});

app.Run();

// DTO san pham
record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price
);

// DTO nhan du lieu them san pham
record CreateProductRequest(
    string Name,
    string Description,
    decimal Price
);

// DTO thong bao loi
record ApiError(
    string Code,
    string Message
);
