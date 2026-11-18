using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

var apiUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5100";

using var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

try
{
    await GetProductsAsync();
    await CreateProductAsync();
    await GetProductAsync(1);
    await CreateInvalidProductAsync();
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Khong the ket noi den API tai {apiUrl}: {ex.Message}");
    Console.WriteLine("Hay kiem tra API dang chay va dia chi may chu.");
}
catch (TaskCanceledException)
{
    Console.WriteLine("Yeu cau bi huy hoac API khong phan hoi trong thoi gian cho phep.");
}

async Task GetProductsAsync()
{
    Console.WriteLine("\n=== GET /api/products ===");
    using var response = await httpClient.GetAsync("/api/products");
    if (!response.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(response);
        return;
    }

    var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(jsonOptions)
        ?? new List<ProductDto>();
    foreach (var product in products)
        Console.WriteLine($"Id: {product.Id} | Ten: {product.Name} | Gia: {product.Price}");
}

async Task CreateProductAsync()
{
    Console.WriteLine("\n=== POST /api/products (hop le) ===");
    var request = new CreateProductRequest("Lap trinh gRPC", "Tai lieu bai 5", 80000);
    using var response = await httpClient.PostAsJsonAsync("/api/products", request, jsonOptions);
    if (response.StatusCode == HttpStatusCode.Created)
    {
        var product = await response.Content.ReadFromJsonAsync<ProductDto>(jsonOptions);
        Console.WriteLine($"Da tao: Id {product?.Id} | {product?.Name} | Gia: {product?.Price}");
        Console.WriteLine($"Location: {response.Headers.Location}");
        return;
    }

    await PrintApiErrorAsync(response);
}

async Task GetProductAsync(int id)
{
    Console.WriteLine($"\n=== GET /api/products/{id} ===");
    using var response = await httpClient.GetAsync($"/api/products/{id}");
    if (!response.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(response);
        return;
    }

    var product = await response.Content.ReadFromJsonAsync<ProductDto>(jsonOptions);
    if (product is not null)
        Console.WriteLine($"Id: {product.Id} | Ten: {product.Name} | Mo ta: {product.Description} | Gia: {product.Price}");
}

async Task CreateInvalidProductAsync()
{
    Console.WriteLine("\n=== POST /api/products (du lieu sai) ===");
    var request = new CreateProductRequest("", "", -1);
    using var response = await httpClient.PostAsJsonAsync("/api/products", request, jsonOptions);
    if (response.StatusCode == HttpStatusCode.BadRequest)
    {
        await PrintApiErrorAsync(response);
        return;
    }

    Console.WriteLine($"May chu phan hoi HTTP {(int)response.StatusCode} {response.StatusCode}.");
    if (response.IsSuccessStatusCode)
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    else
        await PrintApiErrorAsync(response);
}

async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>(jsonOptions);
    Console.WriteLine($"May chu phan hoi HTTP {(int)response.StatusCode} {response.StatusCode}.");
    if (error is not null)
        Console.WriteLine($"{error.Code}: {error.Message}");
    else
        Console.WriteLine(await response.Content.ReadAsStringAsync());
}

record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);
