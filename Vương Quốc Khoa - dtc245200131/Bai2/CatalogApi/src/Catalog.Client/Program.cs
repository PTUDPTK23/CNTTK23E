using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

try
{
    // Bước 1: lấy danh sách sản phẩm
    var products = await http.GetFromJsonAsync<List<ProductDto>>("/api/products");
    Console.WriteLine("Danh sach san pham:");
    foreach (var p in products ?? []) {
        Console.WriteLine($" [{p.Id}] {p.Name} - {p.Price:N0} VND");
    }

    // Bước 2: tạo sản phẩm mới
    var createRequest = new CreateProductRequest(
        "Nhap mon REST API", "Sach tham khao", 120000);

    var createResponse = await http.PostAsJsonAsync("/api/products", createRequest);

    if (createResponse.IsSuccessStatusCode) 
    {
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        Console.WriteLine($"\nDa tao san pham moi: [{created?.Id}] {created?.Name}");
        Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");
    } 
    else 
    {
        var errorText = await createResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"\nLoi {createResponse.StatusCode}: {errorText}");
    }

    // --- ĐOẠN MỚI THÊM VÀO TỪ BƯỚC 8 (SAU KHỐI IF/ELSE) ---

    // Bước 3: đọc lại chi tiết một sản phẩm
    var detail = await http.GetFromJsonAsync<ProductDto>("/api/products/1");
    Console.WriteLine($"\nChi tiet san pham 1: {detail?.Name}");

    // Bước 4: cố ý gửi dữ liệu sai để quan sát lỗi 400
    var invalidResponse = await http.PostAsJsonAsync("/api/products",
        new CreateProductRequest("", "", -1));

    Console.WriteLine($"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    var responseText = await invalidResponse.Content.ReadAsStringAsync();
    if (!string.IsNullOrWhiteSpace(responseText))
    {
        try
        {
            var invalidError = System.Text.Json.JsonSerializer.Deserialize<ApiError>(responseText, 
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Console.WriteLine($"{invalidError?.Code}: {invalidError?.Message}");
        }
        catch
        {
            Console.WriteLine(responseText);
        }
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\nLoi ket noi: Khong the ket noi den Server tai {apiBaseUrl}. Vui long kiem tra API dang chay hay chua.");
    Console.WriteLine($"Chi tiet loi: {ex.Message}");
}

// --- KHU VỰC CUỐI TỆP: KHAI BÁO CÁC RECORD ---
record ProductDto(int Id, string Name, string Description, decimal Price);
record CreateProductRequest(string Name, string? Description, decimal Price);
record ApiError(string Code, string Message);