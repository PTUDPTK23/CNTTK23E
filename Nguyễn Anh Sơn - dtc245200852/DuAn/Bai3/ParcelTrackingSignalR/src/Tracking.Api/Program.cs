using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
// Việc 1: đăng ký dịch vụ SignalR vào DI (đặt TRƯỚC builder.Build())
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(
    new { service = "TrackingApi", status = "running" }));

// Việc 2: ánh xạ hub vào tuyến /hubs/tracking
app.MapHub<TrackingHub>("/hubs/tracking");

app.Run();

sealed class TrackingHub : Hub
{
    private static readonly Dictionary<string, string> Parcels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VN20260000852"] = "CREATED | Nguyen Anh Son -> Tran Van B | Tuyen: HUB-TN01 -> HUB-HCM01",
        ["VN20260000853"] = "IN_TRANSIT | Dang trung chuyen tai HUB-HN01",
        ["VN20260000854"] = "OUT_FOR_DELIVERY | Buu ta dang giao tai HUB-HCM01"
    };

    // Máy khách gọi TrackParcel -> máy chủ trả lời RIÊNG người vừa gọi
    public async Task TrackParcel(string trackingCode)
    {
        var result = Parcels.TryGetValue(trackingCode, out var info)
            ? info
            : "NOT_FOUND | Khong tim thay ma van don tren he thong";

        await Clients.Caller.SendAsync("TrackingResponse", $"[{trackingCode}] {result}");
    }
}
