using System.Net;
using System.Net.Http.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Doc URL API tu command-line argument hoac mac dinh http://localhost:5000
string baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";

Console.WriteLine("========================================================================");
Console.WriteLine("   HE THONG THU VIEN VA DAT MUON SACH PHAN TAN (DE TAI 20)");
Console.WriteLine($"   Target API: {baseUrl}");
Console.WriteLine("========================================================================\n");

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

try
{
    // ========================================================================
    // BUOC 1: GOI GET /books VA IN DANH SACH SACH
    // ========================================================================
    Console.WriteLine(">>> BUOC 1: GOI GET /books (Danh sach sach trong thu vien)...");
    var getBooksResponse = await httpClient.GetAsync("/books");

    if (getBooksResponse.IsSuccessStatusCode)
    {
        var books = await getBooksResponse.Content.ReadFromJsonAsync<List<BookDto>>();
        Console.WriteLine($"[HTTP {(int)getBooksResponse.StatusCode} {getBooksResponse.StatusCode}] Lay thanh cong {books?.Count ?? 0} cuon sach:\n");
        Console.WriteLine("{0,-4} | {1,-35} | {2,-18} | {3,-15} | {4,-6} | {5,-6}",
            "ID", "Tieu de", "Tac gia", "The loai", "Tong", "San co");
        Console.WriteLine(new string('-', 95));

        if (books != null)
        {
            foreach (var b in books)
            {
                Console.WriteLine("{0,-4} | {1,-35} | {2,-18} | {3,-15} | {4,-6} | {5,-6}",
                    b.Id,
                    Truncate(b.Title, 35),
                    Truncate(b.Author, 18),
                    Truncate(b.Category, 15),
                    b.TotalCopies,
                    b.AvailableCopies);
            }
        }
    }
    else
    {
        await HandleHttpErrorAsync(getBooksResponse);
    }
    Console.WriteLine();

    // ========================================================================
    // BUOC 2: CHON CUON SACH (ID = 1) VA GOI GET /books/{id} HIEN THI CHI TIET
    // ========================================================================
    int selectedBookId = 1;
    Console.WriteLine($">>> BUOC 2: GOI GET /books/{selectedBookId} (Chi tiet cuon sach ID = {selectedBookId})...");
    var getDetailResponse = await httpClient.GetAsync($"/books/{selectedBookId}");

    if (getDetailResponse.IsSuccessStatusCode)
    {
        var book = await getDetailResponse.Content.ReadFromJsonAsync<BookDto>();
        Console.WriteLine($"[HTTP {(int)getDetailResponse.StatusCode} {getDetailResponse.StatusCode}] Thong tin chi tiet:");
        if (book != null)
        {
            Console.WriteLine($"  - Ma sach (ID):         {book.Id}");
            Console.WriteLine($"  - Tieu de:              {book.Title}");
            Console.WriteLine($"  - Tac gia:              {book.Author}");
            Console.WriteLine($"  - Ma ISBN:              {book.ISBN}");
            Console.WriteLine($"  - The loai:             {book.Category}");
            Console.WriteLine($"  - Mo ta:                {book.Description}");
            Console.WriteLine($"  - Tong so ban sach:     {book.TotalCopies}");
            Console.WriteLine($"  - So ban dang kha dung: {book.AvailableCopies}");
        }
    }
    else
    {
        await HandleHttpErrorAsync(getDetailResponse);
    }
    Console.WriteLine();

    // ========================================================================
    // BUOC 3 & 4: GUI POST /books/{bookId}/reservations TAO YEU CAU DAT MUON
    // ========================================================================
    Console.WriteLine($">>> BUOC 3 & 4: GUI POST /books/{selectedBookId}/reservations (Dat muon sach ID = {selectedBookId})...");
    var validReservationRequest = new CreateReservationRequest(
        UserId: "dtc245200156",
        UserName: "Giang Hong Anh"
    );

    var postResponse = await httpClient.PostAsJsonAsync($"/books/{selectedBookId}/reservations", validReservationRequest);

    // Kiem tra Status Code truoc khi doc Response Body
    Console.WriteLine($"[HTTP {(int)postResponse.StatusCode} {postResponse.StatusCode}]");

    if (postResponse.StatusCode == HttpStatusCode.Created)
    {
        var reservation = await postResponse.Content.ReadFromJsonAsync<ReservationDto>();
        if (reservation != null)
        {
            Console.WriteLine("  ==> DAT MUON THANH CONG!");
            Console.WriteLine($"  - Ma yeu cau (Reservation Id): {reservation.Id}");
            Console.WriteLine($"  - Ma sach (BookId):            {reservation.BookId}");
            Console.WriteLine($"  - Ma nguoi dung (UserId):      {reservation.UserId}");
            Console.WriteLine($"  - Ten nguoi dung (UserName):   {reservation.UserName}");
            Console.WriteLine($"  - Trang thai (Status):         {reservation.Status}");
            Console.WriteLine($"  - Thoi diem dat (RequestedAt): {reservation.RequestedAt:yyyy-MM-dd HH:mm:ss zzz}");
        }

        // Lay header Location tu phan hoi
        var locationHeader = postResponse.Headers.Location;
        Console.WriteLine($"  - Header Location:             {locationHeader}");

        if (locationHeader != null)
        {
            Console.WriteLine($"\n>>> DOC LAI YEU CAU THEO LOCATION: GET {locationHeader}...");
            var getByLocationResponse = await httpClient.GetAsync(locationHeader);
            if (getByLocationResponse.IsSuccessStatusCode)
            {
                var reloaded = await getByLocationResponse.Content.ReadFromJsonAsync<ReservationDto>();
                Console.WriteLine($"[HTTP {(int)getByLocationResponse.StatusCode} {getByLocationResponse.StatusCode}] Xac nhan doc lai thanh cong: Id={reloaded?.Id}, Status={reloaded?.Status}");
            }
            else
            {
                await HandleHttpErrorAsync(getByLocationResponse);
            }
        }
    }
    else
    {
        await HandleHttpErrorAsync(postResponse);
    }
    Console.WriteLine();

    // ========================================================================
    // BUOC 5: GOI GET /users/{userId}/reservations LAY DANH SACH CUA NGUOI DUNG
    // ========================================================================
    string targetUserId = "dtc245200156";
    Console.WriteLine($">>> BUOC 5: GOI GET /users/{targetUserId}/reservations (Cac yeu cau cua {targetUserId})...");
    var getUserReservationsResponse = await httpClient.GetAsync($"/users/{targetUserId}/reservations");

    if (getUserReservationsResponse.IsSuccessStatusCode)
    {
        var userReservations = await getUserReservationsResponse.Content.ReadFromJsonAsync<List<ReservationDto>>();
        Console.WriteLine($"[HTTP {(int)getUserReservationsResponse.StatusCode} {getUserReservationsResponse.StatusCode}] Tim thay {userReservations?.Count ?? 0} yeu cau:\n");

        if (userReservations != null && userReservations.Count > 0)
        {
            Console.WriteLine("{0,-38} | {1,-7} | {2,-15} | {3,-10} | {4}",
                "Reservation ID", "BookId", "UserId", "Status", "RequestedAt");
            Console.WriteLine(new string('-', 95));
            foreach (var r in userReservations)
            {
                Console.WriteLine("{0,-38} | {1,-7} | {2,-15} | {3,-10} | {4:yyyy-MM-dd HH:mm:ss}",
                    r.Id, r.BookId, r.UserId, r.Status, r.RequestedAt);
            }
        }
        else
        {
            Console.WriteLine("  (Chua co yeu cau dat muon nao)");
        }
    }
    else
    {
        await HandleHttpErrorAsync(getUserReservationsResponse);
    }
    Console.WriteLine();

    // ========================================================================
    // KIEM THU CAC TRUONG HOP LOI NGHIEP VU (KHONG LAM SAP CHUONG TRINH)
    // ========================================================================
    Console.WriteLine("========================================================================");
    Console.WriteLine("   KIEM THU CAC TRUONG HOP LOI (HTTP 400, 404 - DOC APIERROR)");
    Console.WriteLine("========================================================================\n");

    // Lỗi 1: GET sách không tồn tại (ID 999)
    Console.WriteLine(">>> TEST LOI 1: GET /books/999 (Sach khong ton tai)...");
    var notFoundBookResp = await httpClient.GetAsync("/books/999");
    await HandleHttpErrorAsync(notFoundBookResp);
    Console.WriteLine();

    // Lỗi 2: POST thiếu thông tin người dùng
    Console.WriteLine(">>> TEST LOI 2: POST /books/1/reservations (Thieu UserId / UserName)...");
    var invalidUserResp = await httpClient.PostAsJsonAsync("/books/1/reservations", new CreateReservationRequest("", ""));
    await HandleHttpErrorAsync(invalidUserResp);
    Console.WriteLine();

    // Lỗi 3: POST đặt mượn sách không tồn tại
    Console.WriteLine(">>> TEST LOI 3: POST /books/999/reservations (Sach khong ton tai)...");
    var notFoundBookPostResp = await httpClient.PostAsJsonAsync("/books/999/reservations", new CreateReservationRequest("dtc245200156", "Giang Hong Anh"));
    await HandleHttpErrorAsync(notFoundBookPostResp);
    Console.WriteLine();

    // Lỗi 4: POST đặt mượn sách hết bản khả dụng (Book ID = 3, AvailableCopies = 0)
    Console.WriteLine(">>> TEST LOI 4: POST /books/3/reservations (Sach het ban kha dung)...");
    var unavailableResp = await httpClient.PostAsJsonAsync("/books/3/reservations", new CreateReservationRequest("dtc245200156", "Giang Hong Anh"));
    await HandleHttpErrorAsync(unavailableResp);
    Console.WriteLine();

    // Lỗi 5: GET Reservation không tồn tại
    Guid fakeId = Guid.NewGuid();
    Console.WriteLine($">>> TEST LOI 5: GET /reservations/{fakeId} (Reservation khong ton tai)...");
    var notFoundResResp = await httpClient.GetAsync($"/reservations/{fakeId}");
    await HandleHttpErrorAsync(notFoundResResp);
    Console.WriteLine();

    Console.WriteLine("========================================================================");
    Console.WriteLine("   HOAN THANH KICH BAN CLIENT THANH CONG 100%!");
    Console.WriteLine("========================================================================");
}
catch (HttpRequestException ex)
{
    // Xu ly rieng biet: LOI MANG (Network / Connection error)
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[LOI MANG / KET NOI]: Khong the ket noi den server tai {baseUrl}.");
    Console.WriteLine($"Chi tiet: {ex.Message}");
    Console.WriteLine("Vui long dam bao Library.Api dang chay tren cong tuong ung!");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[LOI HE THONG]: {ex.Message}");
    Console.ResetColor();
}

// ====== HAM TRO GIUP XU LY LOI HTTP ======
static async Task HandleHttpErrorAsync(HttpResponseMessage response)
{
    int statusCode = (int)response.StatusCode;
    Console.WriteLine($"[HTTP {statusCode} {response.StatusCode}] Server tra ve ma loi.");

    try
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        if (error != null)
        {
            Console.WriteLine($"  -> ApiError.Code:    {error.Code}");
            Console.WriteLine($"  -> ApiError.Message: {error.Message}");
        }
        else
        {
            var raw = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"  -> Raw Response: {raw}");
        }
    }
    catch
    {
        var raw = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"  -> Khong the parse ApiError. Raw content: {raw}");
    }
}

static string Truncate(string value, int maxLength)
{
    if (string.IsNullOrEmpty(value)) return "";
    return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
}

// ====== CLIENT DTO MODELS ======
public record BookDto(
    int Id,
    string Title,
    string Author,
    string ISBN,
    string Category,
    string Description,
    int TotalCopies,
    int AvailableCopies
);

public record ReservationDto(
    Guid Id,
    int BookId,
    string UserId,
    string UserName,
    string Status,
    DateTimeOffset RequestedAt
);

public record CreateReservationRequest(
    string? UserId,
    string? UserName
);

public record ApiError(
    string Code,
    string Message
);
