using System.Net.Http.Json;

var client = new HttpClient();

client.BaseAddress = new Uri("http://localhost:5100");

// =========================
// GET danh sách sản phẩm
// =========================
var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");

Console.WriteLine("=== DANH SÁCH SẢN PHẨM ===");

if (products != null)
{
    foreach (var p in products)
    {
        Console.WriteLine($"{p.Id} - {p.Name} - {p.Price}");
    }
}

// =========================
// GET sản phẩm theo ID
// =========================
var product = await client.GetFromJsonAsync<ProductDto>("/api/products/2");

Console.WriteLine("\n=== CHI TIẾT SẢN PHẨM ===");

if (product != null)
{
    Console.WriteLine($"{product.Id} - {product.Name} - {product.Description} - {product.Price}");
}

// =========================
// POST thêm sản phẩm
// =========================
Console.WriteLine("\n=== THÊM SẢN PHẨM ===");

var newProduct = new ProductDto(
    5,
    "Speaker",
    "Loa Bluetooth",
    990000
);

var postResponse = await client.PostAsJsonAsync("/api/products", newProduct);

Console.WriteLine($"Status: {postResponse.StatusCode}");

// =========================
// PUT cập nhật sản phẩm
// =========================
Console.WriteLine("\n=== CẬP NHẬT SẢN PHẨM ===");

var updateProduct = new ProductDto(
    2,
    "Mouse Gaming",
    "Logitech G102",
    350000
);

var putResponse = await client.PutAsJsonAsync("/api/products/2", updateProduct);

Console.WriteLine($"Status: {putResponse.StatusCode}");

// =========================
// DELETE sản phẩm
// =========================
Console.WriteLine("\n=== XÓA SẢN PHẨM ===");

var deleteResponse = await client.DeleteAsync("/api/products/5");

Console.WriteLine($"Status: {deleteResponse.StatusCode}");

Console.WriteLine("\nHoàn thành các thao tác REST API!");

// =========================
// DTO
// =========================
record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price
);