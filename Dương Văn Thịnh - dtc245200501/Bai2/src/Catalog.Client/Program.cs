
using System.Net.Http.Json;

var http = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5100")
};

// 1. GET danh sach san pham
Console.WriteLine("=== DANH SACH SAN PHAM ===");

var products = await http.GetFromJsonAsync<List<ProductDto>>(
    "/api/products"
);

if (products != null)
{
    foreach (var product in products)
    {
        Console.WriteLine(
            $"{product.Id}. {product.Name} - {product.Price:N0} VND"
        );
    }
}

// 2. POST them san pham
Console.WriteLine();
Console.WriteLine("=== THEM SAN PHAM ===");

var newProduct = new CreateProductRequest(
    "San pham tu Catalog.Client",
    "Du lieu gui tu HttpClient",
    250000
);

var response = await http.PostAsJsonAsync(
    "/api/products",
    newProduct
);

Console.WriteLine($"HTTP Status: {(int)response.StatusCode}");

if (response.IsSuccessStatusCode)
{
    var createdProduct =
        await response.Content.ReadFromJsonAsync<ProductDto>();

    if (createdProduct != null)
    {
        Console.WriteLine($"ID: {createdProduct.Id}");
        Console.WriteLine($"Ten: {createdProduct.Name}");
        Console.WriteLine($"Gia: {createdProduct.Price:N0} VND");

        // 3. GET san pham vua tao theo ID
        Console.WriteLine();
        Console.WriteLine("=== TRA CUU SAN PHAM ===");

        var foundProduct =
            await http.GetFromJsonAsync<ProductDto>(
                $"/api/products/{createdProduct.Id}"
            );

        if (foundProduct != null)
        {
            Console.WriteLine($"Tim thay: {foundProduct.Name}");
            Console.WriteLine("Tra cuu thanh cong!");
        }
    }
}
else
{
    Console.WriteLine("Them san pham that bai!");
    Console.WriteLine(await response.Content.ReadAsStringAsync());
}

record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price
);

record CreateProductRequest(
    string Name,
    string Description,
    decimal Price
);
