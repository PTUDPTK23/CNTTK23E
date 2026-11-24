// ==============================================================================
// HỆ THỐNG ĐẶT MÓN VÀ XỬ LÝ TẠI BẾP (FOOD ORDERING AND KITCHEN PROCESSING)
// FoodOrdering.KitchenClient - Bảng điều phối nhà bếp thời gian thực (KDS Console)
// Phạm vi kiến thức: Bài 3 - Giao tiếp thời gian thực với SignalR Client
// ==============================================================================

using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5227";
var isAutoMode = args.Any(a => a.Equals("--auto", StringComparison.OrdinalIgnoreCase));

Console.WriteLine("=================================================================");
Console.WriteLine("  HỆ THỐNG ĐẶT MÓN - KITCHEN CLIENT (ĐIỀU PHỐI BẾP SIGNALR)");
Console.WriteLine($"  Kết nối tới Server: {apiBaseUrl}");
Console.WriteLine($"  Chế độ: {(isAutoMode ? "TỰ ĐỘNG XỬ LÝ (--auto)" : "TƯƠNG TÁC THỦ CÔNG")}");
Console.WriteLine("=================================================================\n");

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

try
{
    // ── BƯỚC 1: KHỞI TẠO KẾT NỐI SIGNALR TỚI HUB ─────────────────────────────
    var hubUrl = $"{apiBaseUrl.TrimEnd('/')}/hubs/orders";
    var hubConnection = new HubConnectionBuilder()
        .WithUrl(hubUrl)
        .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
        .Build();

    // ── BƯỚC 2: ĐĂNG KÝ SỰ KIỆN NHẬN THÔNG BÁO VÀ VÒNG ĐỜI KẾT NỐI ───────────

    // 2.1. Xác nhận đã vào nhóm bếp
    hubConnection.On<JoinKitchenConfirmationDto>("JoinKitchenConfirmed", confirm =>
    {
        Console.WriteLine($"  [Hub Xác Nhận] {confirm.Message} (Kênh: {confirm.GroupName})");
    });

    // 2.2. LẮNG NGHE SỰ KIỆN CÓ ĐƠN ĐẶT MÓN MỚI TỪ KHÁCH HÀNG: NewOrderPlaced
    hubConnection.On<OrderDto>("NewOrderPlaced", async newOrder =>
    {
        Console.WriteLine("\n=================================================================");
        Console.WriteLine($"  🔔 [TING TING - CÓ ĐƠN HÀNG MỚI VÀO BẾP!] ĐƠN #{newOrder.Id}");
        Console.WriteLine($"  -> Vị trí: {newOrder.TableNumber} | Khách hàng: {newOrder.CustomerName}");
        Console.WriteLine($"  -> Tổng tiền: {newOrder.TotalAmount:N0} VNĐ | Trạng thái: [{newOrder.Status}]");
        Console.WriteLine($"  -> Thời gian đặt: {newOrder.CreatedAt:HH:mm:ss}");
        Console.WriteLine("  Danh sách món yêu cầu chế biến:");
        foreach (var it in newOrder.Items)
        {
            Console.WriteLine($"     * {it.MenuItemName} x{it.Quantity} = {it.SubTotal:N0} VNĐ");
        }
        Console.WriteLine("=================================================================");
        PrintMenuPrompt();

        if (isAutoMode)
        {
            // Tự động nhận nấu sau 2.5 giây, hoàn thành sau 3 giây để client kịp quan sát các nấc
            _ = Task.Run(async () =>
            {
                await Task.Delay(2500);
                Console.WriteLine($"\n[AUTO] Bếp tự động bắt đầu nấu đơn #{newOrder.Id}...");
                var startRes = await http.PostAsync($"/api/kitchen/orders/{newOrder.Id}/start", null);
                if (startRes.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AUTO] Đơn #{newOrder.Id} đang nấu (Preparing)! Đã gửi SignalR.");
                }

                await Task.Delay(3000);
                Console.WriteLine($"\n[AUTO] Bếp tự động hoàn thành nấu đơn #{newOrder.Id}...");
                var compRes = await http.PostAsync($"/api/kitchen/orders/{newOrder.Id}/complete", null);
                if (compRes.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AUTO] Đơn #{newOrder.Id} đã nấu xong (Ready)! Đã gửi SignalR.");
                }
                PrintMenuPrompt();
            });
        }
    });

    // 2.3. Vòng đời kết nối tự động kết nối lại
    hubConnection.Reconnecting += error =>
    {
        Console.WriteLine($"\n[CẢNH BÁO MẠNG BẾP] Mất kết nối tới Server Hub ({error?.Message}). Đang tự động kết nối lại...");
        return Task.CompletedTask;
    };

    hubConnection.Reconnected += async connectionId =>
    {
        Console.WriteLine($"\n[PHỤC HỒI THÀNH CÔNG] Bếp đã kết nối lại tới Hub! ConnectionId mới: {connectionId}");
        Console.WriteLine("-> Tự động đăng ký lại vào kênh điều phối 'kitchen'...");
        await hubConnection.InvokeAsync("JoinKitchen");
    };

    hubConnection.Closed += error =>
    {
        Console.WriteLine($"\n[KẾT THÚC KẾT NỐI BẾP] Lỗi: {error?.Message ?? "Client chủ động thoát"}");
        return Task.CompletedTask;
    };

    // ── BƯỚC 3: MỞ KẾT NỐI VÀ THAM GIA NHÓM KITCHEN ───────────────────────────
    await hubConnection.StartAsync();
    Console.WriteLine($"  Đã kết nối thành công tới Hub! ConnectionId: {hubConnection.ConnectionId}");

    Console.WriteLine(">>> [BƯỚC 3] Gọi RPC JoinKitchen để tham gia nhóm nhận đơn:");
    await hubConnection.InvokeAsync("JoinKitchen");

    // ── BƯỚC 4: VÒNG LẶP MENU ĐIỀU PHỐI TẠI BẾP ──────────────────────────────
    Console.WriteLine("\nBảng điều phối nhà bếp sẵn sàng tiếp nhận và xử lý đơn hàng!");
    PrintMenu();

    while (true)
    {
        PrintMenuPrompt();
        var choice = Console.ReadLine()?.Trim();
        if (choice is null) continue;

        if (choice.Equals("q", StringComparison.OrdinalIgnoreCase) || choice.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        switch (choice)
        {
            case "1":
                Console.Write("Nhập mã đơn hàng cần BẮT ĐẦU chế biến: ");
                if (int.TryParse(Console.ReadLine(), out var startId))
                {
                    Console.WriteLine($"-> Gửi yêu cầu REST POST /api/kitchen/orders/{startId}/start...");
                    var startRes = await http.PostAsync($"/api/kitchen/orders/{startId}/start", null);
                    if (startRes.IsSuccessStatusCode)
                    {
                        var updated = await startRes.Content.ReadFromJsonAsync<OrderDto>();
                        Console.WriteLine($"\n  [BẾP ĐÃ NHẬN NẤU] Đơn #{updated?.Id} chuyển sang trạng thái: [{updated?.Status}]");
                        Console.WriteLine("  -> Server đã tự động phát thông báo SignalR xuống đúng Thẻ rung của khách hàng!");
                    }
                    else
                    {
                        await PrintApiErrorAsync(startRes);
                    }
                }
                else
                {
                    Console.WriteLine("Mã đơn không hợp lệ!");
                }
                break;

            case "2":
                Console.Write("Nhập mã đơn hàng đã HOÀN THÀNH chế biến: ");
                if (int.TryParse(Console.ReadLine(), out var compId))
                {
                    Console.WriteLine($"-> Gửi yêu cầu REST POST /api/kitchen/orders/{compId}/complete...");
                    var compRes = await http.PostAsync($"/api/kitchen/orders/{compId}/complete", null);
                    if (compRes.IsSuccessStatusCode)
                    {
                        var updated = await compRes.Content.ReadFromJsonAsync<OrderDto>();
                        Console.WriteLine($"\n  [MÓN ĐÃ NẤU XONG] Đơn #{updated?.Id} chuyển sang trạng thái: [{updated?.Status}]!");
                        Console.WriteLine("  -> Server đã kích hoạt rung chuông thời gian thực xuống Thẻ rung của khách hàng!");
                    }
                    else
                    {
                        await PrintApiErrorAsync(compRes);
                    }
                }
                else
                {
                    Console.WriteLine("Mã đơn không hợp lệ!");
                }
                break;

            case "3":
                Console.WriteLine("-> Gửi yêu cầu REST GET /api/kitchen/orders...");
                var listRes = await http.GetAsync("/api/kitchen/orders");
                if (listRes.IsSuccessStatusCode)
                {
                    var allOrders = await listRes.Content.ReadFromJsonAsync<List<OrderDto>>();
                    Console.WriteLine($"\n=== DANH SÁCH TOÀN BỘ ĐƠN HÀNG TẠI BẾP ({allOrders?.Count ?? 0} đơn) ===");
                    foreach (var ord in allOrders ?? [])
                    {
                        Console.WriteLine($"  * Đơn #{ord.Id} | Bàn: {ord.TableNumber} | Khách: {ord.CustomerName} | Trạng thái: [{ord.Status}] | Tiền: {ord.TotalAmount:N0} đ | Cập nhật: {ord.UpdatedAt:HH:mm:ss}");
                    }
                    Console.WriteLine("===============================================================");
                }
                else
                {
                    await PrintApiErrorAsync(listRes);
                }
                break;

            case "help":
            case "h":
                PrintMenu();
                break;

            default:
                Console.WriteLine("Lựa chọn không hợp lệ. Gõ 1, 2, 3 hoặc 'help' để xem hướng dẫn.");
                break;
        }
    }

    await hubConnection.StopAsync();
    Console.WriteLine("Bếp đã đóng ca làm việc. Tạm biệt!");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n[LỖI KẾT NỐI HTTP] Không thể kết nối đến máy chủ tại {apiBaseUrl}.");
    Console.WriteLine($"Chi tiết: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"\n[LỖI HỆ THỐNG] {ex.Message}");
}

static void PrintMenu()
{
    Console.WriteLine("\n--- MENU THAO TÁC CỦA ĐẦU BẾP ---");
    Console.WriteLine("  [1] Bắt đầu chế biến đơn (POST /api/kitchen/orders/{id}/start)");
    Console.WriteLine("  [2] Hoàn thành chế biến đơn (POST /api/kitchen/orders/{id}/complete)");
    Console.WriteLine("  [3] Xem danh sách đơn hiện có (GET /api/kitchen/orders)");
    Console.WriteLine("  [h] Xem lại menu này");
    Console.WriteLine("  [q] Thoát ứng dụng");
    Console.WriteLine("---------------------------------");
}

static void PrintMenuPrompt()
{
    Console.Write("\n[Bếp] Chọn thao tác (1=Start, 2=Complete, 3=List, h=Help, q=Quit): ");
}

static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;

    try
    {
        var error = await r.Content.ReadFromJsonAsync<ApiError>();
        if (error is not null && !string.IsNullOrWhiteSpace(error.Code))
        {
            Console.WriteLine($"  [Lỗi API] Mã: {error.Code} | Thông báo: {error.Message}");
            return;
        }
    }
    catch
    {
        // ignored
    }

    var text = await r.Content.ReadAsStringAsync();
    if (!string.IsNullOrWhiteSpace(text))
    {
        Console.WriteLine($"  [Chi tiết phản hồi]: {text}");
    }
}

// ── KHAI BÁO DTO RIÊNG PHÍA KITCHEN CLIENT ────────────────────────────────────
record OrderItemDto(int MenuItemId, string MenuItemName, decimal UnitPrice, int Quantity, decimal SubTotal);
record OrderDto(
    int Id,
    string CustomerName,
    string TableNumber,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
record JoinKitchenConfirmationDto(string GroupName, string Message);
record ApiError(string Code, string Message);
