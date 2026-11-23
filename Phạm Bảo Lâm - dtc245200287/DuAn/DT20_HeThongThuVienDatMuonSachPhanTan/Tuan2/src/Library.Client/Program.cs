using System.Net;
using System.Net.Http.Json;

var baseUrl = Environment.GetEnvironmentVariable("LIBRARY_API_URL") ?? "http://localhost:8080";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(10) };
Console.WriteLine($"Library Client -> {baseUrl}");

while (true)
{
    Console.WriteLine("\n1) Danh sách sách  2) Chi tiết sách  3) Đặt mượn  4) Trạng thái yêu cầu  5) Yêu cầu của user  0) Thoát");
    Console.Write("> ");
    try
    {
        switch (Console.ReadLine())
        {
            case "1": await ListBooksAsync(); break;
            case "2": await ShowBookAsync(); break;
            case "3": await ReserveAsync(); break;
            case "4": await ShowReservationAsync(); break;
            case "5": await ListUserReservationsAsync(); break;
            case "0": return;
        }
    }
    catch (HttpRequestException ex) { Console.WriteLine($"Không kết nối được API: {ex.Message}"); }
    catch (TaskCanceledException) { Console.WriteLine("API phản hồi quá chậm (timeout)."); }
}

int ReadInt(string label)
{
    Console.Write($"{label}: ");
    return int.TryParse(Console.ReadLine(), out var n) ? n : 0;
}

async Task ListBooksAsync()
{
    var books = await http.GetFromJsonAsync<List<BookDto>>("/books") ?? [];
    foreach (var b in books)
        Console.WriteLine($"[{b.Id}] {b.Title} - {b.Author} | còn {b.AvailableCopies}/{b.TotalCopies}");
}

async Task ShowBookAsync()
{
    var res = await http.GetAsync($"/books/{ReadInt("Id sách")}");
    if (res.StatusCode == HttpStatusCode.NotFound) { Console.WriteLine("Không tìm thấy sách."); return; }
    res.EnsureSuccessStatusCode();
    var b = await res.Content.ReadFromJsonAsync<BookDto>();
    Console.WriteLine($"{b!.Title} - {b.Author} | khả dụng {b.AvailableCopies}/{b.TotalCopies}");
}

async Task ReserveAsync()
{
    var req = new CreateReservationRequest(ReadInt("UserId"), ReadInt("BookId"));
    var res = await http.PostAsJsonAsync("/reservations", req);
    if (res.StatusCode != HttpStatusCode.Created)
    {
        Console.WriteLine($"Lỗi {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return;
    }
    var r = await res.Content.ReadFromJsonAsync<ReservationDto>();
    Console.WriteLine($"Đã gửi yêu cầu {r!.Id} (trạng thái {r.Status}). Đang chờ worker xử lý...");

    // Xử lý bất đồng bộ nên client hỏi lại (polling) tối đa 10 lần, mỗi lần cách 1 giây
    for (var i = 0; i < 10 && r!.Status == "Pending"; i++)
    {
        await Task.Delay(1000);
        r = await http.GetFromJsonAsync<ReservationDto>($"/reservations/{r.Id}");
    }
    Console.WriteLine($"Kết quả: {r!.Status} {r.Reason}");
}

async Task ShowReservationAsync()
{
    Console.Write("Reservation Id: ");
    var res = await http.GetAsync($"/reservations/{Console.ReadLine()}");
    Console.WriteLine(res.IsSuccessStatusCode
        ? (await res.Content.ReadFromJsonAsync<ReservationDto>())!.ToString()
        : $"Lỗi {(int)res.StatusCode}");
}

async Task ListUserReservationsAsync()
{
    var res = await http.GetAsync($"/users/{ReadInt("UserId")}/reservations");
    if (!res.IsSuccessStatusCode) { Console.WriteLine($"Lỗi {(int)res.StatusCode}"); return; }
    foreach (var r in await res.Content.ReadFromJsonAsync<List<ReservationDto>>() ?? [])
        Console.WriteLine($"{r.Id} | {r.BookTitle} | {r.Status} {r.Reason}");
}

// Client có DTO riêng, không tham chiếu Library.Shared: chỉ biết server qua hợp đồng JSON/HTTP
record BookDto(int Id, string Title, string Author, int TotalCopies, int AvailableCopies);
record CreateReservationRequest(int UserId, int BookId);
record ReservationDto(Guid Id, int UserId, int BookId, string? BookTitle, string Status,
    string? Reason, DateTime CreatedAt, DateTime UpdatedAt);
