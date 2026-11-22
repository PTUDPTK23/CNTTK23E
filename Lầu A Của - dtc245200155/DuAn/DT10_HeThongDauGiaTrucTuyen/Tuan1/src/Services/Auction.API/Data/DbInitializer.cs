using AuctionSystem.Shared.Models;

namespace Auction.API.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AuctionDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (context.Auctions.Any())
        {
            return; // Đã có dữ liệu
        }

        var now = DateTime.UtcNow;

        var sampleAuctions = new List<AuctionSystem.Shared.Models.Auction>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "MacBook Pro 16 inch M3 Max 64GB 1TB",
                Description = "Siêu phẩm máy trạm cao cấp nhất của Apple, tình trạng mới 100% nguyên seal chưa kích hoạt bảo hành.",
                ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=60",
                StartingPrice = 65000000,
                CurrentPrice = 72000000,
                StepPrice = 1000000,
                CurrentWinner = "NguyenVanA",
                StartTime = now.AddMinutes(-30),
                EndTime = now.AddHours(2), // Đang mở đấu giá
                Status = AuctionStatus.Active,
                CreatedAt = now.AddMinutes(-35),
                Bids = new List<Bid>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuctionId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Bidder = "TranMinhB",
                        Amount = 66000000,
                        BidTime = now.AddMinutes(-20),
                        IsAccepted = true
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuctionId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Bidder = "LeThiC",
                        Amount = 68000000,
                        BidTime = now.AddMinutes(-10),
                        IsAccepted = true
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuctionId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Bidder = "NguyenVanA",
                        Amount = 72000000,
                        BidTime = now.AddMinutes(-2),
                        IsAccepted = true
                    }
                }
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Đồng hồ Thụy Sĩ Rolex Submariner Date 41mm",
                Description = "Đồng hồ chính hãng Thụy Sĩ Oystersteel, mặt số đen viền Cerachrom, đầy đủ hộp sổ thẻ chính hãng.",
                ImageUrl = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=800&auto=format&fit=crop&q=60",
                StartingPrice = 250000000,
                CurrentPrice = 285000000,
                StepPrice = 5000000,
                CurrentWinner = "PhamHoangD",
                StartTime = now.AddMinutes(-15),
                EndTime = now.AddHours(1), // Đang mở đấu giá
                Status = AuctionStatus.Active,
                CreatedAt = now.AddMinutes(-20),
                Bids = new List<Bid>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuctionId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Bidder = "PhamHoangD",
                        Amount = 285000000,
                        BidTime = now.AddMinutes(-5),
                        IsAccepted = true
                    }
                }
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Title = "Bức tranh sơn mài cổ 'Mùa gặt Tây Bắc' - Họa sĩ Nguyễn Sáng",
                Description = "Tác phẩm nghệ thuật sơn mài truyền thống quý hiếm sáng tác năm 1978, có giấy chứng nhận thẩm định của Bảo tàng Mỹ thuật.",
                ImageUrl = "https://images.unsplash.com/photo-1579783902614-a3fb3927b675?w=800&auto=format&fit=crop&q=60",
                StartingPrice = 120000000,
                CurrentPrice = 120000000,
                StepPrice = 2000000,
                CurrentWinner = null,
                StartTime = now.AddMinutes(-5),
                EndTime = now.AddMinutes(45), // Đang mở đấu giá
                Status = AuctionStatus.Active,
                CreatedAt = now.AddMinutes(-10),
                Bids = new List<Bid>()
            },
            new()
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Title = "Điện thoại iPhone 16 Pro Max 512GB Desert Titanium",
                Description = "Phiên bản cao cấp nhất khung Titan sa mạc, camera cải tiến, chip Apple A18 Pro.",
                ImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=800&auto=format&fit=crop&q=60",
                StartingPrice = 38000000,
                CurrentPrice = 43000000,
                StepPrice = 500000,
                CurrentWinner = "VuThiE",
                StartTime = now.AddHours(-3),
                EndTime = now.AddMinutes(-5), // Đã kết thúc
                Status = AuctionStatus.Ended,
                Winner = "VuThiE",
                CreatedAt = now.AddHours(-4),
                Bids = new List<Bid>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuctionId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                        Bidder = "VuThiE",
                        Amount = 43000000,
                        BidTime = now.AddMinutes(-10),
                        IsAccepted = true
                    }
                }
            }
        };

        context.Auctions.AddRange(sampleAuctions);
        await context.SaveChangesAsync();
    }
}
