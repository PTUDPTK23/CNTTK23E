using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5200";

using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

// Nac 1: lay danh sach sach
var books = await http.GetFromJsonAsync<List<BookDto>>("/api/books");
Console.WriteLine("Danh sach sach:");
foreach (var b in books ?? []) {
    Console.WriteLine($"  [{b.Id}] {b.Title} - {b.Author} ({b.PublishYear})");
}

// Nac 2: tao sach moi bang POST, kiem tra ma 201 truoc khi doc body
var createRequest = new CreateBookRequest(
    "Domain-Driven Design", "Eric Evans", 2003);

var createResponse = await http.PostAsJsonAsync("/api/books", createRequest);

if (createResponse.StatusCode != HttpStatusCode.Created) {
    Console.WriteLine($"\nKhong the them sach: {(int)createResponse.StatusCode} {createResponse.StatusCode}");
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<BookDto>();
Console.WriteLine($"\nDa them sach moi: [{created?.Id}] {created?.Title}");
Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");

// Nac 3: doc lai chi tiet sach vua tao theo Location
var locationUri = createResponse.Headers.Location?.ToString() ?? $"/api/books/{created?.Id}";
var detail = await http.GetFromJsonAsync<BookDto>(locationUri);
Console.WriteLine($"Chi tiet sach vua tao: {detail?.Title} - {detail?.Author} ({detail?.PublishYear})");

// Nac 4: co y gui du lieu sai de quan sat loi 400
var invalidResponse = await http.PostAsJsonAsync(
    "/api/books",
    new CreateBookRequest("", "", -1));

Console.WriteLine($"\nThu gui du lieu sai: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage r) {
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Khai bao DTO (ban sao hop dong JSON cua may chu)
record BookDto(int Id, string Title, string Author, int PublishYear);
record CreateBookRequest(string Title, string Author, int PublishYear);
record ApiError(string Code, string Message);
