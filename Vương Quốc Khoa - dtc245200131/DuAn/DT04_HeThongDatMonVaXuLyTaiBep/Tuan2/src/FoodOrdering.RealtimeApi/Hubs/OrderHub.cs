using Microsoft.AspNetCore.SignalR;
using FoodOrdering.RealtimeApi.Models;

namespace FoodOrdering.RealtimeApi.Hubs;

public class OrderHub : Hub
{
    private readonly ILogger<OrderHub> _logger;

    public OrderHub(ILogger<OrderHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Khách hàng tham gia nhóm theo dõi đơn hàng riêng biệt: order:{orderId}
    /// </summary>
    public async Task WatchOrder(int orderId)
    {
        var groupName = $"order:{orderId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("[Hub] Client {ConnectionId} da tham gia theo doi don #{OrderId} (nhom: {GroupName})",
            Context.ConnectionId, orderId, groupName);

        await Clients.Caller.SendAsync("WatchOrderConfirmed", new
        {
            OrderId = orderId,
            GroupName = groupName,
            Message = $"Ban da tham gia theo doi don hang #{orderId} thanh cong."
        });
    }

    /// <summary>
    /// Khách hàng rời nhóm khi đơn hàng đã hoàn tất
    /// </summary>
    public async Task LeaveOrder(int orderId)
    {
        var groupName = $"order:{orderId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("[Hub] Client {ConnectionId} da roi khoi nhom don #{OrderId}", Context.ConnectionId, orderId);
    }

    /// <summary>
    /// Nhân viên bếp tham gia kênh điều phối chung: kitchen
    /// </summary>
    public async Task JoinKitchen()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "kitchen");
        _logger.LogInformation("[Hub] Bep (Client {ConnectionId}) da tham gia nhom 'kitchen'", Context.ConnectionId);

        await Clients.Caller.SendAsync("JoinKitchenConfirmed", new
        {
            GroupName = "kitchen",
            Message = "Da ket noi vao kenh dieu phoi bep thanh cong."
        });
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("[Hub] + {ConnectionId} ket noi thanh cong.", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("[Hub] - {ConnectionId} ngat ket noi. Ly do: {Reason}",
            Context.ConnectionId, exception?.Message ?? "Client dong chu dong");
        await base.OnDisconnectedAsync(exception);
    }
}
