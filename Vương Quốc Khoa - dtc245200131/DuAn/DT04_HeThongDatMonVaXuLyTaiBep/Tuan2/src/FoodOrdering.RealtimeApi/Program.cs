// ==============================================================================
// HỆ THỐNG ĐẶT MÓN VÀ XỬ LÝ TẠI BẾP (FOOD ORDERING AND KITCHEN PROCESSING)
// FoodOrdering.RealtimeApi - ASP.NET Core 10 Minimal API + SignalR Hub
// Phạm vi kiến thức: Bài 3 - Giao tiếp thời gian thực trong ứng dụng phân tán
// ==============================================================================

using Microsoft.AspNetCore.SignalR;
using FoodOrdering.RealtimeApi.Hubs;
using FoodOrdering.RealtimeApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── CẤU HÌNH DỊCH VỤ SIGNALR & CORS ──────────────────────────────────────────
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

var app = builder.Build();
app.UseCors();

// ── DỮ LIỆU MẪU TRONG BỘ NHỚ (IN-MEMORY STORAGE & THREAD SAFETY) ──────────────
var menuItems = new List<MenuItemDto>
{
    new(1, "Pho Bo Tai Lan", "Dac san Ha Noi thom ngon gia truyen", 55000, true),
    new(2, "Bun Cha Ha Noi", "Thit nuong than hoa dam vi kem nuoc cham", 50000, true),
    new(3, "Com Rang Dua Bo", "Com rang gion rum, dua chua bo xao dam da", 45000, true),
    new(4, "Banh Mi Chao", "Day du pate, trung op la, xuc xich nong hoi", 40000, false) // Het mon
};

var orders = new List<OrderDto>();
var syncLock = new object();

// ── ENDPOINT GỐC: KIỂM TRA TRẠNG THÁI SERVER VÀ SIGNALR ───────────────────────
app.MapGet("/", () => Results.Ok(new
{
    service = "FoodOrdering.RealtimeApi",
    status = "running",
    realtime = "SignalR Enabled",
    hub = "/hubs/orders",
    serverTime = DateTimeOffset.UtcNow
}));

// ── NHÓM 1: QUẢN LÝ THỰC ĐƠN (MENU ITEMS - REST) ─────────────────────────────

// GET /api/menu-items : Lấy danh sách toàn bộ món ăn
app.MapGet("/api/menu-items", () => Results.Ok(menuItems));

// GET /api/menu-items/{id:int} : Lấy thông tin chi tiết một món ăn
app.MapGet("/api/menu-items/{id:int}", (int id) =>
{
    var item = menuItems.FirstOrDefault(m => m.Id == id);
    return item is null
        ? Results.NotFound(new ApiError("ITEM_NOT_FOUND", $"Khong tim thay mon an voi ID = {id}."))
        : Results.Ok(item);
});

// ── NHÓM 2: ĐẶT MÓN VÀ THEO DÕI ĐƠN HÀNG (ORDERS - REST + SIGNALR) ───────────

// POST /api/orders : Khách hàng tạo đơn đặt món mới (REST) -> Push SignalR sang Bếp
app.MapPost("/api/orders", async (CreateOrderRequest request, IHubContext<OrderHub> hubContext) =>
{
    // Cổng 1: Kiểm tra thông tin người đặt và vị trí bàn
    if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.TableNumber))
    {
        return Results.BadRequest(new ApiError("INVALID_ORDER", "Ten khach hang va so ban khong duoc de trong."));
    }

    // Cổng 2: Kiểm tra danh sách món ăn đặt
    if (request.Items is null || request.Items.Count == 0)
    {
        return Results.BadRequest(new ApiError("INVALID_ORDER", "Don hang phai chua it nhat mot mon an."));
    }

    // Cổng 3: Kiểm tra tính hợp lệ của từng món ăn (tồn tại, còn món, số lượng > 0)
    var detailedItems = new List<OrderItemDto>();
    decimal total = 0;

    foreach (var itemReq in request.Items)
    {
        if (itemReq.Quantity <= 0)
        {
            return Results.BadRequest(new ApiError("INVALID_ORDER", $"So luong cua mon an ID {itemReq.MenuItemId} phai lon hon 0."));
        }

        var menuItem = menuItems.FirstOrDefault(m => m.Id == itemReq.MenuItemId);
        if (menuItem is null)
        {
            return Results.BadRequest(new ApiError("INVALID_ORDER", $"Mon an voi ID {itemReq.MenuItemId} khong ton tai trong thuc don."));
        }

        if (!menuItem.IsAvailable)
        {
            return Results.BadRequest(new ApiError("INVALID_ORDER", $"Mon an '{menuItem.Name}' hien tai da het hang."));
        }

        var subTotal = menuItem.Price * itemReq.Quantity;
        detailedItems.Add(new OrderItemDto(menuItem.Id, menuItem.Name, menuItem.Price, itemReq.Quantity, subTotal));
        total += subTotal;
    }

    OrderDto createdOrder;
    lock (syncLock)
    {
        var newId = orders.Count == 0 ? 1 : orders.Max(o => o.Id) + 1;
        var now = DateTimeOffset.UtcNow;
        createdOrder = new OrderDto(
            Id: newId,
            CustomerName: request.CustomerName.Trim(),
            TableNumber: request.TableNumber.Trim(),
            Items: detailedItems,
            TotalAmount: total,
            Status: "Pending",
            CreatedAt: now,
            UpdatedAt: now
        );
        orders.Add(createdOrder);
    }

    // BẮN SỰ KIỆN THỜI GIAN THỰC SANG NHÓM BẾP (kitchen)
    await hubContext.Clients.Group("kitchen").SendAsync("NewOrderPlaced", createdOrder);

    // Trả về HTTP 201 Created kèm header Location trỏ đến /api/orders/{id}
    return Results.Created($"/api/orders/{createdOrder.Id}", createdOrder);
});

// GET /api/orders/{id:int} : Xem chi tiết đơn hàng (REST - Nguồn sự thật)
app.MapGet("/api/orders/{id:int}", (int id) =>
{
    lock (syncLock)
    {
        var order = orders.FirstOrDefault(o => o.Id == id);
        return order is null
            ? Results.NotFound(new ApiError("ORDER_NOT_FOUND", $"Khong tim thay don hang voi ID = {id}."))
            : Results.Ok(order);
    }
});

// ── NHÓM 3: XỬ LÝ ĐƠN HÀNG TẠI BẾP (KITCHEN PROCESSING - REST + SIGNALR) ─────

// GET /api/kitchen/orders : Nhân viên bếp xem danh sách đơn hàng
app.MapGet("/api/kitchen/orders", () =>
{
    lock (syncLock)
    {
        return Results.Ok(orders.ToList());
    }
});

// POST /api/kitchen/orders/{id:int}/start : Bếp xác nhận bắt đầu chế biến
app.MapPost("/api/kitchen/orders/{id:int}/start", async (int id, IHubContext<OrderHub> hubContext) =>
{
    OrderDto updated;
    lock (syncLock)
    {
        var index = orders.FindIndex(o => o.Id == id);
        if (index == -1)
        {
            return Results.NotFound(new ApiError("ORDER_NOT_FOUND", $"Khong tim thay don hang voi ID = {id}."));
        }

        var order = orders[index];
        if (order.Status != "Pending")
        {
            return Results.BadRequest(new ApiError("INVALID_ORDER_STATE",
                $"Don hang ID {id} dang o trang thai '{order.Status}', khong the bat dau che bien (yeu cau 'Pending')."));
        }

        // Cập nhật trạng thái sang "Preparing"
        var now = DateTimeOffset.UtcNow;
        updated = order with { Status = "Preparing", UpdatedAt = now };
        orders[index] = updated;
    }

    // BẮN SỰ KIỆN THỜI GIAN THỰC XUỐNG ĐÚNG NHÓM KHÁCH HÀNG: order:{id}
    var notification = new OrderStatusNotification(
        OrderId: updated.Id,
        Status: updated.Status,
        Message: $"Bếp đã nhận đơn #{updated.Id} và đang bắt đầu chế biến món ăn!",
        UpdatedAt: updated.UpdatedAt
    );
    await hubContext.Clients.Group($"order:{id}").SendAsync("OrderStatusChanged", notification);

    return Results.Ok(updated);
});

// POST /api/kitchen/orders/{id:int}/complete : Bếp hoàn thành chế biến
app.MapPost("/api/kitchen/orders/{id:int}/complete", async (int id, IHubContext<OrderHub> hubContext) =>
{
    OrderDto updated;
    lock (syncLock)
    {
        var index = orders.FindIndex(o => o.Id == id);
        if (index == -1)
        {
            return Results.NotFound(new ApiError("ORDER_NOT_FOUND", $"Khong tim thay don hang voi ID = {id}."));
        }

        var order = orders[index];
        if (order.Status != "Preparing")
        {
            return Results.BadRequest(new ApiError("INVALID_ORDER_STATE",
                $"Don hang ID {id} phai o trang thai 'Preparing' moi co the hoan thanh (hien tai la '{order.Status}')."));
        }

        // Cập nhật trạng thái sang "Ready"
        var now = DateTimeOffset.UtcNow;
        updated = order with { Status = "Ready", UpdatedAt = now };
        orders[index] = updated;
    }

    // BẮN SỰ KIỆN THỜI GIAN THỰC XUỐNG ĐÚNG NHÓM KHÁCH HÀNG: order:{id}
    var notification = new OrderStatusNotification(
        OrderId: updated.Id,
        Status: updated.Status,
        Message: $"Đơn hàng #{updated.Id} đã được nấu xong và sẵn sàng phục vụ! Kính mời quý khách nhận món.",
        UpdatedAt: updated.UpdatedAt
    );
    await hubContext.Clients.Group($"order:{id}").SendAsync("OrderStatusChanged", notification);

    return Results.Ok(updated);
});

// ── ÁNH XẠ HUB SIGNALR ───────────────────────────────────────────────────────
app.MapHub<OrderHub>("/hubs/orders");

app.Run();
