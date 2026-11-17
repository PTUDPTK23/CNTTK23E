var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var products = new List<ProductDto>
{
    new (1,"Giao dich lap trinh phan tan","Giao dich chinh thuc",150000),
    new (2,"Tai lieu RESTful API","Huong dan thiet ke API",90000),
    new (3,"Bai tap thuc hanh .NET","Bo bai tap kem loi giai",120000)
};

//app.MapGet("/", () => "Hello World!");
// GET // : Kiểm tra mát chủ đang chạy
//Result.ok(...) : trả mã 200 ok kèm dữ liệu.Ghi rõ "OK" để người đọc biết ý định
//new{ service = ...,status = ...   }:Đối tượng vô danh :Không cần khai báo lớp. ASP.NET tự đổi thành JSON
//Tên trong JSON: tự đổi sang camecalss
app.MapGet("/",() => Results.Ok(new{
    service = "Catalog.Api",
    status = "Running",
}));
//GET /api/products : danh sách sản phẩm
app.MapGet("/api/products",() => Results.Ok(products));
// Get /api/products/{id:int} : chi tiet mot san pham
app.MapGet("/api/products/{id:int}",(int id) => {
    var product = products.FirstOrDefault(p => p.Id == id);
   return product is null
    ? Results.NotFound(new ApiError("PRODUCT_NOT_FOUND","Khong tim thay san pham."))
    : Results.Ok(product);
});
// POST /api/products : tạo mới từ body JSON
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    // Kiểm tra dữ liệu hợp lệ (Dùng cho Nấc 4)
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.BadRequest(new ApiError("INVALID_PRODUCT", "Ten san pham khong duoc de trong va gia phai lon hon 0."));
    }

    // Tạo sản phẩm mới (Dùng cho Nấc 2)
    var nextId = products.Count == 0 ? 1 : products.Max(p => p.Id) + 1;
    var newProduct = new ProductDto(nextId, request.Name.Trim(), request.Description?.Trim() ?? "", request.Price);
    products.Add(newProduct);

    return Results.Created($"/api/products/{newProduct.Id}", newProduct);
});
//...Phần tạo sản phẩm: slide sau...
// (tiếp sau khối if, trước dấu }); )
app.Run();

public record CreateProductRequest(string Name, string? Description, decimal Price);
public record ProductDto(int Id, string Name, string Description, decimal Price);
public record ApiError(string Code, string Message);