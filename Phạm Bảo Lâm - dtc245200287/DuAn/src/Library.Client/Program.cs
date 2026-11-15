using System.Net.Http.Json;

var baseUrl = Environment.GetEnvironmentVariable("LIBRARY_API_URL") ?? "http://localhost:8081";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine($"Library Client kết nối tới {baseUrl}");

while (true)
{
    Console.WriteLine("\n1) Danh sách sách  2) Chi tiết sách  3) Đặt mượn  4) Trạng thái yêu cầu  5) Yêu cầu của user  0) Thoát");
    Console.Write("> ");
    var choice = Console.ReadLine()?.Trim();
    if (choice == "0" || choice is null) break;

    try
    {
        switch (choice)
        {
            case "1":
                var books = await http.GetFromJsonAsync<List<BookDto>>("/books") ?? [];
                foreach (var b in books)
                    Console.WriteLine($"[{b.Id}] {b.Title} - {b.Author} | còn {b.AvailableCopies}/{b.TotalCopies}");
                break;

            case "2":
                Console.Write("Id sách: ");
                var bookId = Console.ReadLine()?.Trim();
                var resBook = await http.GetAsync($"/books/{bookId}");
                if (!resBook.IsSuccessStatusCode)
                    Console.WriteLine($"Lỗi {(int)resBook.StatusCode}: Không tìm thấy sách.");
                else
                {
                    var b = await resBook.Content.ReadFromJsonAsync<BookDto>();
                    Console.WriteLine($"[{b!.Id}] {b.Title} - {b.Author} | khả dụng: {b.AvailableCopies}/{b.TotalCopies}");
                }
                break;

            case "3":
                Console.Write("UserId: ");
                int.TryParse(Console.ReadLine(), out var uId);
                Console.Write("BookId: ");
                int.TryParse(Console.ReadLine(), out var bId);

                var resCreate = await http.PostAsJsonAsync("/reservations", new { userId = uId, bookId = bId });
                var body = await resCreate.Content.ReadAsStringAsync();
                Console.WriteLine($"HTTP {(int)resCreate.StatusCode}: {body}");
                break;

            case "4":
                Console.Write("Reservation Id (GUID): ");
                var rId = Console.ReadLine()?.Trim();
                var resStatus = await http.GetAsync($"/reservations/{rId}");
                Console.WriteLine($"HTTP {(int)resStatus.StatusCode}: {await resStatus.Content.ReadAsStringAsync()}");
                break;

            case "5":
                Console.Write("UserId: ");
                var userId = Console.ReadLine()?.Trim();
                var list = await http.GetFromJsonAsync<List<ReservationDto>>($"/users/{userId}/reservations") ?? [];
                foreach (var r in list)
                    Console.WriteLine($"{r.Id} | {r.BookTitle} | {r.Status}");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi kết nối API: {ex.Message}");
    }
}

record BookDto(int Id, string Title, string Author, int TotalCopies, int AvailableCopies);
record ReservationDto(Guid Id, int UserId, int BookId, string BookTitle, string Status);
