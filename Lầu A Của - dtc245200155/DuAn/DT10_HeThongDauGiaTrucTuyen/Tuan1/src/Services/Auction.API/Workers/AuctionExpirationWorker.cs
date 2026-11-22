using Auction.API.Data;
using AuctionSystem.Shared.Common;
using AuctionSystem.Shared.Events;
using AuctionSystem.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Auction.API.Workers;

public class AuctionExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuctionExpirationWorker> _logger;

    public AuctionExpirationWorker(
        IServiceProvider serviceProvider,
        ILogger<AuctionExpirationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuctionExpirationWorker started. Checking for expired auctions every 5s.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

                var now = DateTime.UtcNow;

                // Tìm các phiên đấu giá đang Active nhưng đã quá EndTime
                var expiredAuctions = await dbContext.Auctions
                    .Include(a => a.Bids)
                    .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
                    .ToListAsync(stoppingToken);

                foreach (var auction in expiredAuctions)
                {
                    auction.Status = AuctionStatus.Ended;
                    // Winner là người có bid cao nhất được chấp nhận
                    var highestBid = auction.Bids
                        .Where(b => b.IsAccepted)
                        .OrderByDescending(b => b.Amount)
                        .FirstOrDefault();

                    auction.Winner = highestBid?.Bidder;
                    auction.CurrentWinner = auction.Winner;

                    _logger.LogInformation("Auction [{Id}] '{Title}' ended automatically. Winner: {Winner}, Final Price: {Price:N0}",
                        auction.Id, auction.Title, auction.Winner ?? "No winner", auction.CurrentPrice);

                    var endedEvent = new AuctionEndedEvent
                    {
                        AuctionId = auction.Id,
                        Title = auction.Title,
                        Winner = auction.Winner,
                        FinalPrice = auction.CurrentPrice,
                        TotalBids = auction.Bids.Count(b => b.IsAccepted),
                        EndedAt = now
                    };

                    try
                    {
                        publisher.Publish(RabbitMqConstants.RoutingKeyAuctionEnded, endedEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish AuctionEndedEvent for auction {AuctionId}", auction.Id);
                    }
                }

                if (expiredAuctions.Count > 0)
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in AuctionExpirationWorker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
