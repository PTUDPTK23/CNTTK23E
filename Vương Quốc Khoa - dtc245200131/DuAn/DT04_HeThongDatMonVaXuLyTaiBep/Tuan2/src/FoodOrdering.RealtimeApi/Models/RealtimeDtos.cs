namespace FoodOrdering.RealtimeApi.Models;

public record MenuItemDto(int Id, string Name, string Description, decimal Price, bool IsAvailable);

public record OrderItemRequest(int MenuItemId, int Quantity);

public record CreateOrderRequest(string CustomerName, string TableNumber, List<OrderItemRequest> Items);

public record OrderItemDto(int MenuItemId, string MenuItemName, decimal UnitPrice, int Quantity, decimal SubTotal);

public record OrderDto(
    int Id,
    string CustomerName,
    string TableNumber,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record OrderStatusNotification(
    int OrderId,
    string Status,
    string Message,
    DateTimeOffset UpdatedAt
);

public record ApiError(string Code, string Message);
