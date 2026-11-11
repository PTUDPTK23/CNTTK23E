
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

// Dia chi API
var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5100";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};

try
{
    // BUOC 1: Lay danh sach san pham
    Console.WriteLine("===== DANH SACH SAN PHAM =====");

    var products = await http.GetFromJsonAsync<List<ProductDto>>(
        "/api/products", jsonOptions);

    if (products != null)
    {
        foreach (var p in products)
        {
            Console.WriteLine(
                $"[{p.Id}] {p.Name} - {p.Price:N0} VND");
        }
    }

    // BUOC 2: Tao san pham moi
    Console.WriteLine("\n===== TAO SAN PHAM MOI =====");

    var createRequest = new CreateProductRequest(
        "Nhap mon REST API",
        "Sach tham khao",
        120000
    );

    var createResponse = await http.PostAsJsonAsync(
        "/api/products", createRequest);

    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var created = await createResponse.Content
            .ReadFromJsonAsync<ProductDto>(jsonOptions);

        Console.WriteLine(
            $"Da tao san pham: [{created?.Id}] {created?.Name}");

        Console.WriteLine(
            $"Doc lai tai: {createResponse.Headers.Location}");
    }
    else
    {
        await ShowError(createResponse);
    }

    // BUOC 3: Doc chi tiet san pham
    Console.WriteLine("\n===== CHI TIET SAN PHAM =====");

    var detailResponse = await http.GetAsync("/api/products/1");

    if (detailResponse.IsSuccessStatusCode)
    {
        var detail = await detailResponse.Content
            .ReadFromJsonAsync<ProductDto>(jsonOptions);

        Console.WriteLine($"Ma SP: {detail?.Id}");
        Console.WriteLine($"Ten SP: {detail?.Name}");
        Console.WriteLine($"Mo ta: {detail?.Description}");
        Console.WriteLine($"Gia: {detail?.Price:N0} VND");
    }
    else
    {
        await ShowError(detailResponse);
    }

    // BUOC 4: Gui du lieu sai de kiem tra loi 400
    Console.WriteLine("\n===== KIEM TRA DU LIEU SAI =====");

    var invalidRequest = new CreateProductRequest(
        "",
        "",
        -1
    );

    var invalidResponse = await http.PostAsJsonAsync(
        "/api/products", invalidRequest);

    Console.WriteLine(
        $"Trang thai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");

    if (!invalidResponse.IsSuccessStatusCode)
    {
        await ShowError(invalidResponse);
    }

    // BUOC 5: Ket thuc
    Console.WriteLine("\n===== HOAN THANH =====");
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Khong ket noi duoc API!");
    Console.WriteLine(ex.Message);
}
catch (Exception ex)
{
    Console.WriteLine("Co loi xay ra:");
    Console.WriteLine(ex.Message);
}

// HAM HIEN THI LOI
static async Task ShowError(HttpResponseMessage response)
{
    var content = await response.Content.ReadAsStringAsync();

    Console.WriteLine($"Ma loi: {(int)response.StatusCode}");

    if (string.IsNullOrWhiteSpace(content))
    {
        Console.WriteLine("Khong co noi dung loi.");
        return;
    }

    try
    {
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        if (root.TryGetProperty("code", out var code))
            Console.WriteLine($"Code: {code}");

        if (root.TryGetProperty("message", out var message))
            Console.WriteLine($"Message: {message}");

        if (root.TryGetProperty("title", out var title))
            Console.WriteLine($"Title: {title}");

        if (root.TryGetProperty("detail", out var detail))
            Console.WriteLine($"Detail: {detail}");
    }
    catch (JsonException)
    {
        Console.WriteLine(content);
    }
}

// DTO
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
