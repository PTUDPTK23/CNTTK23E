// ==============================================================================
// HỆ THỐNG ĐẶT MÓN VÀ XỬ LÝ TẠI BẾP (FOOD ORDERING AND KITCHEN PROCESSING)
// FoodOrdering.CustomerClient - Khách hàng đặt món & Thẻ rung điện tử Realtime
// Phạm vi kiến thức: Bài 3 - Giao tiếp thời gian thực với SignalR Client
// ==============================================================================

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Đọc tham số dòng lệnh: [apiBaseUrl] [customerName] [tableNumber] [watchOrderId]
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5227";
var customerName = args.Length > 1 ? args[1] : "Anh Nguyen";
var tableNumber = args.Length > 2 ? args[2] : "Ban 05";
int? preassignedOrderId = args.Length > 3 && int.TryParse(args[3], out var parsedId) ? parsedId : null;

Console.WriteLine("=================================================================");
Console.WriteLine("  HỆ THỐNG ĐẶT MÓN - CUSTOMER CLIENT (THẺ RUNG ĐIỆN TỬ SIGNALR)");
Console.WriteLine($"  Kết nối tới Server: {apiBaseUrl}");
Console.WriteLine($"  Khách hàng: {customerName} | Bàn: {tableNumber}");
Console.WriteLine("=================================================================\n");

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

int currentOrderId;

try
{
    if (preassignedOrderId.HasValue)
    {
        // Chế độ quan sát trực tiếp đơn hàng đã có (hỗ trợ kiểm thử cô lập nhóm / đa máy khách)
        currentOrderId = preassignedOrderId.Value;
        Console.WriteLine($">>> [CHẾ ĐỘ THEO DÕI TRỰC TIẾP] Đơn hàng #{currentOrderId}:");
        var res = await http.GetAsync($"/api/orders/{currentOrderId}");
        if (res.IsSuccessStatusCode)
        {
            var ord = await res.Content.ReadFromJsonAsync<OrderDto>();
            Console.WriteLine($"  Đã tìm thấy đơn #{ord?.Id} ({ord?.CustomerName} - {ord?.TableNumber}) | Trạng thái hiện tại: [{ord?.Status}]");
        }
        else
        {
            Console.WriteLine($"  [Lưu ý] Đơn #{currentOrderId} chưa tồn tại trên Server, Client vẫn sẽ đăng ký theo dõi phòng order:{currentOrderId}.");
        }
    }
    else
    {
        // ── BƯỚC 1: XEM THỰC ĐƠN QUA REST API ────────────────────────────────
        Console.WriteLine(">>> [BƯỚC 1] Khách hàng xem danh sách thực đơn (GET /api/menu-items):");
        var menuResponse = await http.GetAsync("/api/menu-items");
        if (!menuResponse.IsSuccessStatusCode)
        {
            await PrintApiErrorAsync(menuResponse);
            return;
        }

        var menuItems = await menuResponse.Content.ReadFromJsonAsync<List<MenuItemDto>>();
        foreach (var item in menuItems ?? [])
        {
            var statusText = item.IsAvailable ? "Còn món" : "Hết món";
            Console.WriteLine($"  [{item.Id}] {item.Name} - {item.Price:N0} VNĐ ({statusText})");
        }

        // ── BƯỚC 2: ĐẶT MÓN QUA REST API ────────────────────────────────────
        Console.WriteLine("\n>>> [BƯỚC 2] Khách hàng gửi yêu cầu đặt món (POST /api/orders):");
        var orderRequest = new CreateOrderRequest(
            CustomerName: customerName,
            TableNumber: tableNumber,
            Items: new List<OrderItemRequest>
            {
                new(MenuItemId: 1, Quantity: 2), // 2 Phở Bò Tái Lăn (55.000 x 2 = 110.000)
                new(MenuItemId: 2, Quantity: 1)  // 1 Bún Chả Hà Nội   (50.000 x 1 = 50.000)
            }
        );

        var createOrderResponse = await http.PostAsJsonAsync("/api/orders", orderRequest);
        if (createOrderResponse.StatusCode != HttpStatusCode.Created)
        {
            Console.WriteLine($"  Tạo đơn hàng thất bại! Mã HTTP: {(int)createOrderResponse.StatusCode}");
            await PrintApiErrorAsync(createOrderResponse);
            return;
        }

        var createdOrder = await createOrderResponse.Content.ReadFromJsonAsync<OrderDto>();
        currentOrderId = createdOrder?.Id ?? 1;

        Console.WriteLine($"  [THÀNH CÔNG] Đã tạo đơn hàng #{currentOrderId}!");
        Console.WriteLine($"  Khách: {createdOrder?.CustomerName} | Bàn: {createdOrder?.TableNumber}");
        Console.WriteLine($"  Tổng tiền: {createdOrder?.TotalAmount:N0} VNĐ");
        Console.WriteLine($"  Trạng thái ban đầu: [{createdOrder?.Status}]");
        Console.WriteLine($"  Đường dẫn theo dõi (Location Header): {createOrderResponse.Headers.Location}");
    }

    // ── BƯỚC 3: KHỞI TẠO KẾT NỐI SIGNALR (THẺ RUNG ĐIỆN TỬ) ───────────────────
    Console.WriteLine("\n>>> [BƯỚC 3] Khởi tạo kết nối SignalR Thẻ rung điện tử (hubs/orders):");
    var hubUrl = $"{apiBaseUrl.TrimEnd('/')}/hubs/orders";

    var hubConnection = new HubConnectionBuilder()
        .WithUrl(hubUrl)
        .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
        .Build();

    // ── BƯỚC 4: ĐĂNG KÝ CÁC SỰ KIỆN SIGNALR VÀ VÒNG ĐỜI KẾT NỐI ──────────────

    // 4.1. Lắng nghe xác nhận tham gia nhóm theo dõi đơn
    hubConnection.On<WatchConfirmationDto>("WatchOrderConfirmed", confirm =>
    {
        Console.WriteLine($"  [Hub Xác Nhận] {confirm.Message} (Nhóm: {confirm.GroupName})");
    });

    // 4.2. LẮNG NGHE SỰ KIỆN THỜI GIAN THỰC TỪ BẾP: OrderStatusChanged
    hubConnection.On<OrderStatusNotification>("OrderStatusChanged", notif =>
    {
        Console.WriteLine("\n-----------------------------------------------------------------");
        Console.WriteLine($"  🔔 [THẺ RUNG RUNG LÊN!] CẬP NHẬT ĐƠN HÀNG #{notif.OrderId}");
        Console.WriteLine($"  -> Trạng thái mới: [{notif.Status}]");
        Console.WriteLine($"  -> Thông điệp: {notif.Message}");
        Console.WriteLine($"  -> Thời gian cập nhật: {notif.UpdatedAt:HH:mm:ss}");
        Console.WriteLine("-----------------------------------------------------------------");

        if (notif.Status == "Preparing")
        {
            Console.WriteLine("  ⚡ Đèn vàng nhấp nháy: Đầu bếp đang chuẩn bị món ăn cho quý khách...");
        }
        else if (notif.Status == "Ready")
        {
            Console.WriteLine("  🎉 ĐÈN XANH SÁNG & RUNG LIÊN TỤC: MÓN ĂN ĐÃ NẤU XONG!");
            Console.WriteLine($"  >>> Kính mời quý khách tại {tableNumber} mang thẻ rung đến quầy nhận món!");
        }
    });

    // 4.3. Quản lý vòng đời kết nối và tự động phục hồi (Reconnection)
    hubConnection.Reconnecting += error =>
    {
        Console.WriteLine($"\n[CẢNH BÁO MẠNG] Mất kết nối tới Server SignalR ({error?.Message}).");
        Console.WriteLine("Đang tự động thử kết nối lại theo chiến lược WithAutomaticReconnect...");
        return Task.CompletedTask;
    };

    hubConnection.Reconnected += async connectionId =>
    {
        Console.WriteLine($"\n[PHỤC HỒI KẾT NỐI] Đã kết nối lại thành công! ConnectionId mới: {connectionId}");
        Console.WriteLine($"-> Tự động tham gia lại nhóm theo dõi đơn #{currentOrderId}...");
        await hubConnection.InvokeAsync("WatchOrder", currentOrderId);

        // ĐỒNG BỘ NGUỒN SỰ THẬT (SOURCE OF TRUTH) TỪ REST API SAU KHI PHỤC HỒI
        Console.WriteLine("-> Đang kiểm tra lại trạng thái chính thức từ REST API (Nguồn sự thật)...");
        try
        {
            var syncRes = await http.GetAsync($"/api/orders/{currentOrderId}");
            if (syncRes.IsSuccessStatusCode)
            {
                var syncedOrder = await syncRes.Content.ReadFromJsonAsync<OrderDto>();
                Console.WriteLine($"[ĐỒNG BỘ THÀNH CÔNG] Trạng thái trên Server: [{syncedOrder?.Status}] (Cập nhật lúc: {syncedOrder?.UpdatedAt:HH:mm:ss})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LỖI ĐỒNG BỘ REST] {ex.Message}");
        }
    };

    hubConnection.Closed += error =>
    {
        Console.WriteLine($"\n[KẾT NỐI ĐÃ ĐÓNG] Kết nối SignalR kết thúc. Lỗi: {error?.Message ?? "Client chủ động thoát"}");
        return Task.CompletedTask;
    };

    // ── BƯỚC 5: KẾT NỐI TỚI HUB VÀ GỌI RPC THAM GIA NHÓM ĐƠN HÀNG ─────────────
    await hubConnection.StartAsync();
    Console.WriteLine($"  Đã kết nối thành công tới Hub! ConnectionId: {hubConnection.ConnectionId}");

    Console.WriteLine($"\n>>> [BƯỚC 5] Đăng ký theo dõi đơn hàng #{currentOrderId} qua RPC WatchOrder:");
    await hubConnection.InvokeAsync("WatchOrder", currentOrderId);

    // Kiểm tra trạng thái tức thời ngay sau khi vào phòng (phòng trường hợp bếp nhận đơn cực nhanh)
    var initialSyncRes = await http.GetAsync($"/api/orders/{currentOrderId}");
    if (initialSyncRes.IsSuccessStatusCode)
    {
        var initOrd = await initialSyncRes.Content.ReadFromJsonAsync<OrderDto>();
        if (initOrd is not null && initOrd.Status != "Pending")
        {
            Console.WriteLine($"  [TRẠNG THÁI TỨC THỜI] Đơn #{currentOrderId} hiện đã ở trạng thái: [{initOrd.Status}]");
        }
    }

    Console.WriteLine("\n=================================================================");
    Console.WriteLine($"  THẺ RUNG ĐANG HOẠT ĐỘNG! Đang lắng nghe thông báo cho Đơn #{currentOrderId}...");
    Console.WriteLine("  (Nhấn Enter hoặc gõ 'exit' để dừng ứng dụng)");
    Console.WriteLine("=================================================================\n");

    // Giữ kết nối để lắng nghe sự kiện
    while (true)
    {
        var line = Console.ReadLine();
        if (line is not null && (line.Equals("exit", StringComparison.OrdinalIgnoreCase) || line.Equals("q", StringComparison.OrdinalIgnoreCase)))
        {
            break;
        }

        // Cho phép tra cứu lại trạng thái đơn hàng thủ công qua REST
        var checkRes = await http.GetAsync($"/api/orders/{currentOrderId}");
        if (checkRes.IsSuccessStatusCode)
        {
            var ord = await checkRes.Content.ReadFromJsonAsync<OrderDto>();
            Console.WriteLine($"[REST Check] Đơn #{ord?.Id} hiện tại: [{ord?.Status}] (Cập nhật: {ord?.UpdatedAt:HH:mm:ss})");
        }
    }

    // Rời nhóm và ngắt kết nối khi thoát
    await hubConnection.InvokeAsync("LeaveOrder", currentOrderId);
    await hubConnection.StopAsync();
    Console.WriteLine("Đã tắt thẻ rung. Tạm biệt quý khách!");
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

// ── HÀM DÙNG CHUNG IN LỖI API ────────────────────────────────────────────────
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

// ── KHAI BÁO DTO RIÊNG PHÍA CUSTOMER CLIENT ──────────────────────────────────
record MenuItemDto(int Id, string Name, string Description, decimal Price, bool IsAvailable);
record OrderItemRequest(int MenuItemId, int Quantity);
record CreateOrderRequest(string CustomerName, string TableNumber, List<OrderItemRequest> Items);
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
record OrderStatusNotification(int OrderId, string Status, string Message, DateTimeOffset UpdatedAt);
record WatchConfirmationDto(int OrderId, string GroupName, string Message);
record ApiError(string Code, string Message);
