using Microsoft.AspNetCore.SignalR;

namespace Auction.Hub.Hubs;

public class AuctionHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly ILogger<AuctionHub> _logger;

    public AuctionHub(ILogger<AuctionHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinAuction(string auctionId)
    {
        var groupName = $"auction-{auctionId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);

        await Clients.Caller.SendAsync("JoinedSuccess", new { auctionId, message = $"Đã tham gia phòng đấu giá {auctionId}" });
    }

    public async Task LeaveAuction(string auctionId)
    {
        var groupName = $"auction-{auctionId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
