using System.Net.Http.Json;
using AuctionSystem.Shared.DTOs;
using AuctionSystem.Shared.Events;
using Microsoft.AspNetCore.SignalR.Client;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Cấu hình URL qua tham số dòng lệnh hoặc biến môi trường hoặc mặc định
var apiUrl = args.Length > 0 ? args[0] : (Environment.GetEnvironmentVariable("API_URL") ?? "http://localhost:5001");
var hubUrl = args.Length > 1 ? args[1] : (Environment.GetEnvironmentVariable("HUB_URL") ?? "http://localhost:5003");

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(apiUrl),
    Timeout = TimeSpan.FromSeconds(10)
};

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=================================================================");
Console.WriteLine("    HỆ THỐNG ĐẤU GIÁ TRỰC TUYẾN - CONSOLE CLIENT (.NET 10)       ");
Console.WriteLine("=================================================================");
Console.ResetColor();
Console.WriteLine($"BaseAddress API : {httpClient.BaseAddress}");
Console.WriteLine($"SignalR Hub URL : {hubUrl}\n");

// Kiểm tra kết nối dịch vụ ban đầu
await CheckServiceHealthAsync();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n--- MENU CHỨC NĂNG ---");
    Console.WriteLine("1. Xem thông tin dịch vụ (GET /)");
    Console.WriteLine("2. Xem danh sách phiên đấu giá (GET /auctions)");
    Console.WriteLine("3. Xem chi tiết phiên đấu giá (GET /auctions/{id})");
    Console.WriteLine("4. Gửi mức giá mới (POST /auctions/{id}/bids -> Đọc Location -> GET đọc lại)");
    Console.WriteLine("5. Xem lịch sử trả giá của phiên (GET /auctions/{id}/bids)");
    Console.WriteLine("6. Tham gia phòng đấu giá & Nhận giá Realtime (SignalR Hub)");
    Console.WriteLine("7. Chạy Bot tự động trả giá liên tục (Simulation Test)");
    Console.WriteLine("8. Kết thúc phiên đấu giá (POST /auctions/{id}/end)");
    Console.WriteLine("9. Thử nghiệm kiểm tra lỗi kết nối (API offline test)");
    Console.WriteLine("0. Thoát");
    Console.ResetColor();
    Console.Write("\nLựa chọn của bạn: ");
    var choice = Console.ReadLine()?.Trim();

    if (choice == "0") break;

    try
    {
        switch (choice)
        {
            case "1":
                await GetServiceInfoAsync();
                break;
            case "2":
                await ListAuctionsAsync();
                break;
            case "3":
                await ViewAuctionDetailAsync();
                break;
            case "4":
                await PlaceBidWithLocationAsync();
                break;
            case "5":
                await ViewBidHistoryAsync();
                break;
            case "6":
                await JoinRealtimeRoomAsync();
                break;
            case "7":
                await RunAutoBidSimulationAsync();
                break;
            case "8":
                await EndAuctionAsync();
                break;
            case "9":
                await TestApiOfflineAsync();
                break;
            default:
                Console.WriteLine("Lựa chọn không hợp lệ. Vui lòng chọn từ 0 đến 9.");
                break;
        }
    }
    catch (HttpRequestException ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[LỖI KẾT NỐI]: Không thể kết nối tới máy chủ API tại {httpClient.BaseAddress}.");
        Console.WriteLine($"Chi tiết: {ex.Message}");
        Console.WriteLine("Gợi ý: Hãy đảm bảo Auction.API đang chạy trước khi thực hiện thao tác.");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[LỖI HỆ THỐNG]: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine("\nNhấn Enter để tiếp tục...");
    Console.ReadLine();
}

async Task CheckServiceHealthAsync()
{
    try
    {
        var res = await httpClient.GetAsync("/health");
        if (res.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[KẾT NỐI]: Máy chủ API đang hoạt động (Healthcheck OK).");
            Console.ResetColor();
        }
    }
    catch
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("[CẢNH BÁO]: Chưa kết nối được tới API. Bạn vẫn có thể dùng Client sau khi khởi động API.");
        Console.ResetColor();
    }
}

async Task GetServiceInfoAsync()
{
    Console.WriteLine("\nĐang gửi request GET / ...");
    var res = await httpClient.GetAsync("/");
    if (res.IsSuccessStatusCode)
    {
        var info = await res.Content.ReadFromJsonAsync<ServiceInfoDto>();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[HTTP {(int)res.StatusCode} OK]");
        Console.ResetColor();
        Console.WriteLine($"Service     : {info?.Service}");
        Console.WriteLine($"Version     : {info?.Version}");
        Console.WriteLine($"Status      : {info?.Status}");
        Console.WriteLine($"Description : {info?.Description}");
        Console.WriteLine($"Timestamp   : {info?.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
    }
    else
    {
        await HandleApiErrorAsync(res);
    }
}

async Task ListAuctionsAsync()
{
    Console.WriteLine("\nĐang gửi request GET /auctions ...");
    var res = await httpClient.GetAsync("/auctions");

    // Bắt buộc: Kiểm tra StatusCode trước khi đọc dữ liệu
    if (!res.IsSuccessStatusCode)
    {
        await HandleApiErrorAsync(res);
        return;
    }

    var auctions = await res.Content.ReadFromJsonAsync<List<AuctionResponse>>();
    if (auctions == null || auctions.Count == 0)
    {
        Console.WriteLine("Chưa có phiên đấu giá nào trong hệ thống.");
        return;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n[HTTP {(int)res.StatusCode} OK] Tìm thấy {auctions.Count} phiên đấu giá:");
    Console.WriteLine($"{"ID",-36} | {"Tên sản phẩm",-28} | {"Giá hiện tại",-16} | {"Trạng thái",-10} | {"Winner",-15}");
    Console.WriteLine(new string('-', 115));
    Console.ResetColor();

    foreach (var a in auctions)
    {
        Console.WriteLine($"{a.Id,-36} | {Truncate(a.Title, 26),-28} | {a.CurrentPrice:N0} đ{"",-5} | {a.StatusName,-10} | {a.CurrentWinner ?? "Chưa có",-15}");
    }
}

async Task ViewAuctionDetailAsync()
{
    Console.Write("\nNhập ID phiên đấu giá (GUID): ");
    var idStr = Console.ReadLine()?.Trim();
    if (!Guid.TryParse(idStr, out var id))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Mã ID không đúng định dạng GUID.");
        Console.ResetColor();
        return;
    }

    Console.WriteLine($"Đang gửi request GET /auctions/{id} ...");
    var res = await httpClient.GetAsync($"/auctions/{id}");

    // Kiểm tra StatusCode trước khi đọc
    if (!res.IsSuccessStatusCode)
    {
        await HandleApiErrorAsync(res);
        return;
    }

    var a = await res.Content.ReadFromJsonAsync<AuctionResponse>();
    if (a != null)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[HTTP {(int)res.StatusCode} OK]");
        Console.ResetColor();
        Console.WriteLine($"--- THÔNG TIN PHIÊN ĐẤU GIÁ ---");
        Console.WriteLine($"Mã ID         : {a.Id}");
        Console.WriteLine($"Tiêu đề       : {a.Title}");
        Console.WriteLine($"Mô tả         : {a.Description}");
        Console.WriteLine($"Giá khởi điểm : {a.StartingPrice:N0} đ");
        Console.WriteLine($"Giá hiện tại  : {a.CurrentPrice:N0} đ");
        Console.WriteLine($"Bước giá      : +{a.StepPrice:N0} đ");
        Console.WriteLine($"Đang dẫn đầu  : {a.CurrentWinner ?? "Chưa có"}");
        Console.WriteLine($"Trạng thái    : {a.StatusName}");
        Console.WriteLine($"Thời gian     : {a.StartTime:dd/MM HH:mm} -> {a.EndTime:dd/MM HH:mm} UTC");
        Console.WriteLine($"Người thắng   : {a.Winner ?? "Chưa có"}");
    }
}

async Task PlaceBidWithLocationAsync()
{
    Console.Write("\nNhập ID phiên đấu giá: ");
    var idStr = Console.ReadLine()?.Trim();
    if (!Guid.TryParse(idStr, out var id))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Mã ID không hợp lệ.");
        Console.ResetColor();
        return;
    }

    Console.Write("Tên người trả giá (Bidder): ");
    var bidder = Console.ReadLine()?.Trim() ?? string.Empty;

    Console.Write("Mức giá muốn trả (VNĐ): ");
    if (!decimal.TryParse(Console.ReadLine()?.Trim(), out var amount))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Số tiền không hợp lệ.");
        Console.ResetColor();
        return;
    }

    Console.WriteLine($"\n[BƯỚC 1]: Gửi request POST /auctions/{id}/bids (gọi gRPC kiểm tra nghiệp vụ)...");
    var postResponse = await httpClient.PostAsJsonAsync($"/auctions/{id}/bids", new PlaceBidRequest(bidder, amount));

    // Bắt buộc: Kiểm tra StatusCode trước khi đọc
    if (!postResponse.IsSuccessStatusCode)
    {
        await HandleApiErrorAsync(postResponse);
        return;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[HTTP {(int)postResponse.StatusCode} Created] Đặt giá thành công!");
    Console.ResetColor();

    // Bắt buộc: Đọc header Location
    var locationUri = postResponse.Headers.Location;
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Header Location nhận được từ server: {locationUri}");
    Console.ResetColor();

    if (locationUri != null)
    {
        // Bắt buộc: Gọi GET đọc lại lượt nộp/đặt giá theo ID lấy từ Location
        Console.WriteLine($"\n[BƯỚC 2]: Gọi GET {locationUri} để xác nhận dữ liệu đã lưu trữ...");
        var getResponse = await httpClient.GetAsync(locationUri);
        if (getResponse.IsSuccessStatusCode)
        {
            var bidDto = await getResponse.Content.ReadFromJsonAsync<BidResponse>();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[XÁC THỰC THÀNH CÔNG TỪ LOCATION]:");
            Console.ResetColor();
            Console.WriteLine($"  - Lượt nộp ID   : {bidDto?.Id}");
            Console.WriteLine($"  - Phiên đấu giá : {bidDto?.AuctionId}");
            Console.WriteLine($"  - Người đặt giá : {bidDto?.Bidder}");
            Console.WriteLine($"  - Mức giá       : {bidDto?.Amount:N0} đ");
            Console.WriteLine($"  - Thời gian nộp : {bidDto?.BidTime:dd/MM/yyyy HH:mm:ss} UTC");
            Console.WriteLine($"  - Trạng thái    : {(bidDto?.IsAccepted == true ? "HỢP LỆ (Accepted)" : "BỊ TỪ CHỐI")}");
        }
        else
        {
            await HandleApiErrorAsync(getResponse);
        }
    }
}

async Task ViewBidHistoryAsync()
{
    Console.Write("\nNhập ID phiên đấu giá: ");
    var idStr = Console.ReadLine()?.Trim();
    if (!Guid.TryParse(idStr, out var id))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Mã ID không hợp lệ.");
        Console.ResetColor();
        return;
    }

    Console.WriteLine($"Đang gửi request GET /auctions/{id}/bids ...");
    var res = await httpClient.GetAsync($"/auctions/{id}/bids");
    if (!res.IsSuccessStatusCode)
    {
        await HandleApiErrorAsync(res);
        return;
    }

    var bids = await res.Content.ReadFromJsonAsync<List<BidResponse>>();
    if (bids == null || bids.Count == 0)
    {
        Console.WriteLine("Chưa có lượt trả giá nào.");
        return;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n[HTTP {(int)res.StatusCode} OK] Lịch sử {bids.Count} lượt trả giá:");
    Console.ResetColor();
    foreach (var b in bids)
    {
        var status = b.IsAccepted ? "HỢP LỆ" : $"TỪ CHỐI ({b.Reason})";
        Console.WriteLine($"[{b.BidTime:HH:mm:ss}] {b.Bidder,-15}: {b.Amount:N0} đ -> {status}");
    }
}

async Task JoinRealtimeRoomAsync()
{
    Console.Write("\nNhập ID phiên đấu giá muốn theo dõi realtime: ");
    var idStr = Console.ReadLine()?.Trim();
    if (!Guid.TryParse(idStr, out var id))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Mã ID không hợp lệ.");
        Console.ResetColor();
        return;
    }

    var hubConnection = new HubConnectionBuilder()
        .WithUrl($"{hubUrl}/hubs/auction")
        .WithAutomaticReconnect()
        .Build();

    hubConnection.On<BidAcceptedEvent>("ReceiveBidUpdate", bid =>
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n⚡ [REALTIME SIGNALR] >> Bid mới từ '{bid.Bidder}': {bid.Amount:N0} đ (lúc {bid.Timestamp:HH:mm:ss})");
        Console.ResetColor();
    });

    hubConnection.On<AuctionEndedEvent>("ReceiveAuctionEnded", ended =>
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n🏆 [REALTIME SIGNALR] >> PHIÊN ĐÃ KẾT THÚC! Winner: {ended.Winner ?? "Không có"} | Giá chốt: {ended.FinalPrice:N0} đ");
        Console.ResetColor();
    });

    try
    {
        Console.WriteLine("Đang kết nối tới SignalR Hub...");
        await hubConnection.StartAsync();
        await hubConnection.InvokeAsync("JoinAuction", id.ToString());

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[ĐÃ THAM GIA PHÒNG {id}] Đang lắng nghe giá realtime...");
        Console.WriteLine("Nhấn phím bất kỳ để rời phòng và quay lại menu...");
        Console.ResetColor();

        Console.ReadKey();

        await hubConnection.InvokeAsync("LeaveAuction", id.ToString());
        await hubConnection.StopAsync();
        Console.WriteLine("Đã rời phòng đấu giá.");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Lỗi SignalR: {ex.Message}");
        Console.ResetColor();
    }
}

async Task RunAutoBidSimulationAsync()
{
    Console.Write("\nNhập ID phiên đấu giá muốn chạy Bot: ");
    var idStr = Console.ReadLine()?.Trim();
    if (!Guid.TryParse(idStr, out var id))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Mã ID không hợp lệ.");
        Console.ResetColor();
        return;
    }

    var res = await httpClient.GetAsync($"/auctions/{id}");
    if (!res.IsSuccessStatusCode)
    {
        await HandleApiErrorAsync(res);
        return;
    }

    var a = await res.Content.ReadFromJsonAsync<AuctionResponse>();
    if (a == null || a.StatusName != "Active")
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Phiên đấu giá không hợp lệ hoặc đã kết thúc.");
        Console.ResetColor();
        return;
    }

    Console.Write("Số lượt đặt giá tự động (VD: 3): ");
    if (!int.TryParse(Console.ReadLine()?.Trim(), out var count) || count <= 0) count = 3;

    var bots = new[] { "Bot_Alpha", "Bot_Beta", "Bot_Gamma" };
    var currentPrice = a.CurrentPrice;

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n--- BẮT ĐẦU CHẠY MÔ PHỎNG BOT ĐẤU GIÁ ({count} lượt) ---");
    Console.ResetColor();

    for (int i = 0; i < count; i++)
    {
        var botName = bots[i % bots.Length];
        var nextBid = currentPrice + a.StepPrice;

        Console.Write($"[{i + 1}/{count}] {botName} gửi mức giá {nextBid:N0} đ...");
        var postRes = await httpClient.PostAsJsonAsync($"/auctions/{id}/bids", new PlaceBidRequest(botName, nextBid));
        if (postRes.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" THÀNH CÔNG (HTTP 201 Created -> Location: {postRes.Headers.Location})");
            Console.ResetColor();
            currentPrice = nextBid;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" THẤT BẠI: ");
            await HandleApiErrorAsync(postRes);
        }

        await Task.Delay(1500);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("--- KẾT THÚC MÔ PHỎNG ---");
    Console.ResetColor();
}

async Task EndAuctionAsync()
{
    Console.Write("\nNhập ID phiên đấu giá muốn kết thúc sớm: ");
    var idStr = Console.ReadLine()?.Trim();
    if (!Guid.TryParse(idStr, out var id))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Mã ID không hợp lệ.");
        Console.ResetColor();
        return;
    }

    Console.WriteLine($"Đang gửi request POST /auctions/{id}/end ...");
    var res = await httpClient.PostAsync($"/auctions/{id}/end", null);
    if (res.IsSuccessStatusCode)
    {
        var body = await res.Content.ReadAsStringAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[HTTP {(int)res.StatusCode} OK]: {body}");
        Console.ResetColor();
    }
    else
    {
        await HandleApiErrorAsync(res);
    }
}

async Task TestApiOfflineAsync()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n[KIỂM THỬ XỬ LÝ LỖI KẾT NỐI]:");
    Console.WriteLine("Thử gửi request tới một địa chỉ API không tồn tại (giả lập server bị sập)...");
    Console.ResetColor();

    using var deadClient = new HttpClient { BaseAddress = new Uri("http://localhost:59999"), Timeout = TimeSpan.FromSeconds(2) };
    try
    {
        await deadClient.GetAsync("/auctions");
    }
    catch (HttpRequestException ex)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=> BẮT LỖI THÀNH CÔNG: Chương trình không bị crash, đã bắt được HttpRequestException:");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"   Message: {ex.Message}");
        Console.ResetColor();
    }
}

async Task HandleApiErrorAsync(HttpResponseMessage response)
{
    Console.ForegroundColor = ConsoleColor.Red;
    try
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        if (error != null)
        {
            Console.WriteLine($"[LỖI HTTP {(int)response.StatusCode}] Code: {error.Code} | Message: {error.Message}");
            if (!string.IsNullOrEmpty(error.Details))
            {
                Console.WriteLine($"  Chi tiết: {error.Details}");
            }
        }
        else
        {
            var raw = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[LỖI HTTP {(int)response.StatusCode}]: {raw}");
        }
    }
    catch
    {
        Console.WriteLine($"[LỖI HTTP {(int)response.StatusCode}] {response.ReasonPhrase}");
    }
    Console.ResetColor();
}

string Truncate(string val, int maxLength)
{
    if (string.IsNullOrEmpty(val)) return "";
    return val.Length <= maxLength ? val : val.Substring(0, maxLength - 3) + "...";
}
