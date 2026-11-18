
using System.Net;
using System.Net.Http.Json;

// Buoc 1: Cau hinh dia chi may chu API
var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5100";

// Buoc 2: Khoi tao HttpClient
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Buoc 3: GET danh sach san pham
Console.WriteLine("DANH SACH SAN PHAM");

var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");

foreach (var p in products ?? [])
{
    Console.WriteLine($"[{p.Id}] {p.Name} - {p.Price:N0} VND");
}

// Buoc 4: POST tao san pham moi
Console.WriteLine("\nTAO SAN PHAM MOI");

var createRequest = new CreateProductRequest(
    "Nhap mon REST API",
    "Sach tham khao",
    120000
);

var createResponse = await http.PostAsJsonAsync(
    "/api/products",
    createRequest
);

// Buoc 5: Kiem tra ma trang thai HTTP
if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content
        .ReadFromJsonAsync<ProductDto>();

    Console.WriteLine(
        $"\nDa tao san pham moi: [{created?.Id}] {created?.Name}"
    );

    Console.WriteLine(
        $"Doc lai tai: {createResponse.Headers.Location}"
    );
}
else
{
    var error = await createResponse.Content
        .ReadFromJsonAsync<ApiError>();

    Console.WriteLine(
        $"\nLoi: {error?.Code} - {error?.Message}"
    );

    return;
}

// Buoc 6: GET chi tiet mot san pham
var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");

Console.WriteLine($"\nChi tiet san pham 1: {detail?.Name}");

// Buoc 7: Co y gui du lieu sai de kiem tra loi 400
var invalidResponse = await http.PostAsJsonAsync(
    "/api/products",
    new CreateProductRequest("", "", -1)
);

Console.WriteLine(
    $"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}"
);

var invalidError = await invalidResponse.Content
    .ReadFromJsonAsync<ApiError>();

Console.WriteLine(
    $"{invalidError?.Code}: {invalidError?.Message}"
);

// Buoc 8: Khai bao DTO va Request o cuoi tep
record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price
);

record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price
);

record ApiError(
    string Code,
    string Message
);
