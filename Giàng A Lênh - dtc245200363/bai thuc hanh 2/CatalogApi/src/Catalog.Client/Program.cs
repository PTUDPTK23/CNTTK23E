
using System.Net.Http.Json;

// Địa chỉ Backend API
var client = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5100")
};

try
{
    // Gọi API lấy danh sách sản phẩm
    Console.WriteLine("Dang ket noi den Catalog API...");

    var products = await client.GetFromJsonAsync<List<ProductDto>>(
        "/api/products"
    );

    Console.WriteLine("\n===== DANH SACH SAN PHAM =====");

    if (products != null)
    {
        foreach (var product in products)
        {
            Console.WriteLine($"ID: {product.Id}");
            Console.WriteLine($"Ten: {product.Name}");
            Console.WriteLine($"Mo ta: {product.Description}");
            Console.WriteLine($"Gia: {product.Price:N0} VND");
            Console.WriteLine("----------------------------");
        }
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi ket noi API: {ex.Message}");
}

record ProductDto(
    int Id,
    string Name,
    string? Description,
    decimal Price
);
