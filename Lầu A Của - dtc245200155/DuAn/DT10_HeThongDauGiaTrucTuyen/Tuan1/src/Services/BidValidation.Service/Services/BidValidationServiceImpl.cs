using System.Globalization;
using AuctionSystem.Contracts;
using Grpc.Core;

namespace BidValidation.Service.Services;

public class BidValidationServiceImpl : AuctionSystem.Contracts.BidValidation.BidValidationBase
{
    private readonly ILogger<BidValidationServiceImpl> _logger;

    public BidValidationServiceImpl(ILogger<BidValidationServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<ValidateBidResponse> ValidateBid(ValidateBidRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC ValidateBid received for AuctionId: {AuctionId}, Bidder: {Bidder}, Amount: {Amount}, CurrentPrice: {CurrentPrice}",
            request.AuctionId, request.Bidder, request.Amount, request.CurrentPrice);

        // 1. Kiểm tra tên người trả giá
        if (string.IsNullOrWhiteSpace(request.Bidder))
        {
            return Task.FromResult(new ValidateBidResponse
            {
                IsValid = false,
                Reason = "Tên người trả giá không được để trống.",
                MinRequiredPrice = request.CurrentPrice + request.StepPrice
            });
        }

        // 2. Kiểm tra trạng thái phiên đấu giá
        if (!string.Equals(request.AuctionStatus, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ValidateBidResponse
            {
                IsValid = false,
                Reason = $"Phiên đấu giá đang ở trạng thái '{request.AuctionStatus}', không thể đặt giá.",
                MinRequiredPrice = request.CurrentPrice
            });
        }

        // 3. Kiểm tra thời gian
        var now = DateTime.UtcNow;
        if (DateTime.TryParse(request.StartTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var startTime) && now < startTime)
        {
            return Task.FromResult(new ValidateBidResponse
            {
                IsValid = false,
                Reason = $"Phiên đấu giá chưa bắt đầu. Thời gian bắt đầu: {startTime:dd/MM/yyyy HH:mm:ss} UTC.",
                MinRequiredPrice = request.StartingPrice
            });
        }

        if (DateTime.TryParse(request.EndTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var endTime) && now >= endTime)
        {
            return Task.FromResult(new ValidateBidResponse
            {
                IsValid = false,
                Reason = $"Phiên đấu giá đã kết thúc lúc: {endTime:dd/MM/yyyy HH:mm:ss} UTC.",
                MinRequiredPrice = request.CurrentPrice
            });
        }

        // 4. Tính toán mức giá tối thiểu hợp lệ
        double minRequiredPrice;
        if (request.CurrentPrice <= 0)
        {
            minRequiredPrice = request.StartingPrice;
        }
        else
        {
            minRequiredPrice = request.CurrentPrice + (request.StepPrice > 0 ? request.StepPrice : 1000);
        }

        // 5. Kiểm tra mức giá trả
        if (request.Amount < minRequiredPrice)
        {
            _logger.LogWarning("Bid rejected: Amount {Amount} < MinRequired {MinRequiredPrice}",
                request.Amount, minRequiredPrice);

            return Task.FromResult(new ValidateBidResponse
            {
                IsValid = false,
                Reason = $"Mức giá trả ({request.Amount:N0} đ) phải lớn hơn hoặc bằng mức giá tối thiểu ({minRequiredPrice:N0} đ).",
                MinRequiredPrice = minRequiredPrice
            });
        }

        _logger.LogInformation("Bid validated successfully for AuctionId {AuctionId}. Amount: {Amount}",
            request.AuctionId, request.Amount);

        return Task.FromResult(new ValidateBidResponse
        {
            IsValid = true,
            Reason = "Lượt trả giá hợp lệ.",
            MinRequiredPrice = minRequiredPrice
        });
    }
}
