using AuctionSystem.Shared.Models;

namespace AuctionSystem.Shared.DTOs;

#region API Error Model
/// <summary>
/// Chuẩn cấu trúc phản hồi lỗi thống nhất cho toàn bộ hệ thống API
/// </summary>
public record ApiError(string Code, string Message, string? Details = null);

public static class ApiErrorCodes
{
    public const string AuctionNotFound = "AUCTION_NOT_FOUND";
    public const string InvalidAuctionData = "INVALID_AUCTION_DATA";
    public const string BidRejected = "BID_REJECTED";
    public const string AuctionEnded = "AUCTION_ENDED";
    public const string AuctionNotStarted = "AUCTION_NOT_STARTED";
    public const string BidNotFound = "BID_NOT_FOUND";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string InternalError = "INTERNAL_SERVER_ERROR";
}
#endregion

#region Service Info DTO
public record ServiceInfoDto(
    string Service,
    string Version,
    string Status,
    string Description,
    DateTime Timestamp
);
#endregion

#region Input DTOs (Request)
public record CreateAuctionRequest(
    string Title,
    string Description,
    string? ImageUrl,
    decimal StartingPrice,
    decimal StepPrice,
    DateTime StartTime,
    DateTime EndTime
);

public record PlaceBidRequest(
    string Bidder,
    decimal Amount
);
#endregion

#region Output DTOs (Response)
public record AuctionResponse(
    Guid Id,
    string Title,
    string Description,
    string ImageUrl,
    decimal StartingPrice,
    decimal CurrentPrice,
    decimal StepPrice,
    string? CurrentWinner,
    DateTime StartTime,
    DateTime EndTime,
    AuctionStatus Status,
    string StatusName,
    string? Winner,
    int TotalBids,
    DateTime CreatedAt
);

public record BidResponse(
    Guid Id,
    Guid AuctionId,
    string Bidder,
    decimal Amount,
    DateTime BidTime,
    bool IsAccepted,
    string? Reason
);
#endregion
