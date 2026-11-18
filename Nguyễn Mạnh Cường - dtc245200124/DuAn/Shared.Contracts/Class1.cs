namespace Shared.Contracts;

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public sealed class ProductItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public List<ProductItem> Products { get; set; } = new();
}

public sealed class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<ProductItem> Products { get; set; } = new();
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed record ProcessOrderMessage(Guid OrderId);

public sealed record OrderStatusChangedMessage(
    Guid OrderId,
    string CustomerName,
    OrderStatus Status,
    DateTimeOffset UpdatedAt,
    string? Reason);
