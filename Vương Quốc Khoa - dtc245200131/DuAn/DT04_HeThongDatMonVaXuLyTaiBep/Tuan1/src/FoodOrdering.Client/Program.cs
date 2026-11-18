// ==============================================================================
// HỆ THỐNG ĐẶT MÓN VÀ XỬ LÝ TẠI BẾP (FOOD ORDERING AND KITCHEN PROCESSING)
// FoodOrdering.Client - Ứng dụng Console Máy Khách
// Phạm vi kiến thức: Bài 2 - RESTful API cho tương tác Client-Server
// ==============================================================================

using System.Net;
using System.Net.Http.Json;

// Lấy địa chỉ API từ tham số dòng lệnh hoặc mặc định cổng 5055
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5227";

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=================================================================");
Console.WriteLine("  HỆ THỐNG ĐẶT MÓN & XỬ LÝ TẠI BẾP - CONSOLE CLIENT (.NET 10)");
Console.WriteLine($"  Kết nối tới Server: {apiBaseUrl}");
Console.WriteLine("=================================================================\n");

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

try
{
    // ── NẤC 1: KHÁCH HÀNG XEM THỰC ĐƠN ──────────────────────────────────────
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
        Console.WriteLine($"      Mô tả: {item.Description}");
    }

    // ── NẤC 2: KHÁCH HÀNG XEM CHI TIẾT MỘT MÓN ĂN ───────────────────────────
    Console.WriteLine("\n>>> [BƯỚC 2] Xem chi tiết món ăn ID = 1 (GET /api/menu-items/1):");
    var itemDetailResponse = await http.GetAsync("/api/menu-items/1");
    if (itemDetailResponse.IsSuccessStatusCode)
    {
        var item1 = await itemDetailResponse.Content.ReadFromJsonAsync<MenuItemDto>();
        Console.WriteLine($"  Món: {item1?.Name} | Đơn giá: {item1?.Price:N0} VNĐ | Trạng thái: {(item1?.IsAvailable == true ? "Sẵn sàng" : "Tạm hết")}");
    }
    else
    {
        await PrintApiErrorAsync(itemDetailResponse);
    }

    // ── NẤC 3: KHÁCH HÀNG GỬI YÊU CẦU ĐẶT MÓN (POST /api/orders) ────────────
    Console.WriteLine("\n>>> [BƯỚC 3] Khách hàng gửi yêu cầu đặt món (POST /api/orders):");
    var orderRequest = new CreateOrderRequest(
        CustomerName: "Anh Nguyen",
        TableNumber: "Ban 05",
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
    Console.WriteLine($"  [THÀNH CÔNG] Đã tạo đơn hàng #{createdOrder?.Id}!");
    Console.WriteLine($"  Khách: {createdOrder?.CustomerName} | Bàn: {createdOrder?.TableNumber}");
    Console.WriteLine($"  Tổng tiền: {createdOrder?.TotalAmount:N0} VNĐ");
    Console.WriteLine($"  Trạng thái ban đầu: {createdOrder?.Status}");
    Console.WriteLine($"  Đường dẫn theo dõi (Location): {createOrderResponse.Headers.Location}");

    var currentOrderId = createdOrder?.Id ?? 1;

    // ── NẤC 4: KHÁCH HÀNG KIỂM TRA LẠI CHI TIẾT ĐƠN HÀNG VỪA TẠO ────────────
    Console.WriteLine($"\n>>> [BƯỚC 4] Kiểm tra lại đơn hàng #{currentOrderId} (GET /api/orders/{currentOrderId}):");
    var checkOrderResponse = await http.GetAsync($"/api/orders/{currentOrderId}");
    if (checkOrderResponse.IsSuccessStatusCode)
    {
        var orderDetail = await checkOrderResponse.Content.ReadFromJsonAsync<OrderDto>();
        Console.WriteLine($"  Đơn #{orderDetail?.Id} hiện đang ở trạng thái: [{orderDetail?.Status}]");
        foreach (var it in orderDetail?.Items ?? [])
        {
            Console.WriteLine($"    - {it.MenuItemName} x{it.Quantity} = {it.SubTotal:N0} VNĐ");
        }
    }

    // ── NẤC 5: NHÂN VIÊN BẾP XEM DANH SÁCH ĐƠN HÀNG CẦN XỬ LÝ ───────────────
    Console.WriteLine("\n>>> [BƯỚC 5] Bếp xem danh sách đơn hàng (GET /api/kitchen/orders):");
    var kitchenOrdersResponse = await http.GetAsync("/api/kitchen/orders");
    if (kitchenOrdersResponse.IsSuccessStatusCode)
    {
        var allOrders = await kitchenOrdersResponse.Content.ReadFromJsonAsync<List<OrderDto>>();
        Console.WriteLine($"  Số lượng đơn hiện có: {allOrders?.Count ?? 0}");
        foreach (var ord in allOrders ?? [])
        {
            Console.WriteLine($"  * Đơn #{ord.Id} ({ord.TableNumber} - {ord.CustomerName}) -> Trạng thái: {ord.Status}");
        }
    }

    // ── NẤC 6: BẾP XÁC NHẬN BẮT ĐẦU CHẾ BIẾN (POST .../start) ───────────────
    Console.WriteLine($"\n>>> [BƯỚC 6] Bếp bắt đầu chế biến đơn #{currentOrderId} (POST /api/kitchen/orders/{currentOrderId}/start):");
    var startResponse = await http.PostAsync($"/api/kitchen/orders/{currentOrderId}/start", null);
    if (startResponse.IsSuccessStatusCode)
    {
        var preparingOrder = await startResponse.Content.ReadFromJsonAsync<OrderDto>();
        Console.WriteLine($"  [BẾP ĐÃ NHẬN ĐƠN] Trạng thái đơn #{preparingOrder?.Id} đã chuyển sang: [{preparingOrder?.Status}]");
    }
    else
    {
        await PrintApiErrorAsync(startResponse);
    }

    // ── NẤC 7: BẾP HOÀN THÀNH CHẾ BIẾN (POST .../complete) ───────────────────
    Console.WriteLine($"\n>>> [BƯỚC 7] Bếp chế biến xong đơn #{currentOrderId} (POST /api/kitchen/orders/{currentOrderId}/complete):");
    var completeResponse = await http.PostAsync($"/api/kitchen/orders/{currentOrderId}/complete", null);
    if (completeResponse.IsSuccessStatusCode)
    {
        var readyOrder = await completeResponse.Content.ReadFromJsonAsync<OrderDto>();
        Console.WriteLine($"  [MÓN ĐÃ NẤU XONG] Đơn #{readyOrder?.Id} đã sẵn sàng phục vụ: [{readyOrder?.Status}]!");
    }
    else
    {
        await PrintApiErrorAsync(completeResponse);
    }

    // ── NẤC 8: THỬ NGHIỆM PHÁ LỖI & KIỂM TRA MÃ TRẠNG THÁI HTTP ────────────
    Console.WriteLine("\n=================================================================");
    Console.WriteLine(">>> [BƯỚC 8] THỬ NGHIỆM PHÁ LỖI VÀ XỬ LÝ LỖI (ERROR HANDLING):");

    // 8a. Thử hoàn thành lại một đơn đã "Ready" -> Kỳ vọng 400 INVALID_ORDER_STATE
    Console.WriteLine("\n  [8a] Thử bấm 'hoàn thành' lần nữa cho đơn đã Ready:");
    var reCompleteResponse = await http.PostAsync($"/api/kitchen/orders/{currentOrderId}/complete", null);
    Console.WriteLine($"  Mã phản hồi HTTP: {(int)reCompleteResponse.StatusCode} {reCompleteResponse.StatusCode}");
    await PrintApiErrorAsync(reCompleteResponse);

    // 8b. Thử đặt món đã hết hàng (Món 4: Bánh Mì Chảo) -> Kỳ vọng 400 INVALID_ORDER
    Console.WriteLine("\n  [8b] Thử đặt món đã hết hàng (Món ID = 4):");
    var orderUnavailable = new CreateOrderRequest("Khach B", "Ban 02", new List<OrderItemRequest> { new(4, 1) });
    var badOrderResponse = await http.PostAsJsonAsync("/api/orders", orderUnavailable);
    Console.WriteLine($"  Mã phản hồi HTTP: {(int)badOrderResponse.StatusCode} {badOrderResponse.StatusCode}");
    await PrintApiErrorAsync(badOrderResponse);

    // 8c. Thử truy vấn món ăn không tồn tại -> Kỳ vọng 404 ITEM_NOT_FOUND
    Console.WriteLine("\n  [8c] Thử tìm món ăn không tồn tại (ID = 999):");
    var notFoundResponse = await http.GetAsync("/api/menu-items/999");
    Console.WriteLine($"  Mã phản hồi HTTP: {(int)notFoundResponse.StatusCode} {notFoundResponse.StatusCode}");
    await PrintApiErrorAsync(notFoundResponse);

    Console.WriteLine("\n=================================================================");
    Console.WriteLine("  TẤT CẢ CÁC NẤC THỰC THI VÀ XỬ LÝ LỖI ĐÃ HOÀN TẤT THÀNH CÔNG!");
    Console.WriteLine("=================================================================");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n[LỖI KẾT NỐI MẠNG] Không thể kết nối đến Server tại {apiBaseUrl}.");
    Console.WriteLine("Vui lòng kiểm tra máy chủ FoodOrdering.Api đã khởi động chưa!");
    Console.WriteLine($"Chi tiết ngoại lệ: {ex.Message}");
}

// ── HÀM DÙNG CHUNG ĐỌC VÀ HIỂN THỊ APIERROR ────────────────────────────────
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
        // Phản hồi không phải dạng JSON ApiError
    }

    var text = await r.Content.ReadAsStringAsync();
    if (!string.IsNullOrWhiteSpace(text))
    {
        Console.WriteLine($"  [Chi tiết phản hồi]: {text}");
    }
}

// ── KHAI BÁO DTO RIÊNG CỦA MÁY KHÁCH (CLIENT DTOs) ─────────────────────────
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
    DateTimeOffset CreatedAt
);

record ApiError(string Code, string Message);
