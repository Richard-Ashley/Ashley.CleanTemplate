namespace Application.Orders.Queries.GetOrderById;

public sealed record OrderDto(
    Guid Id,
    string CustomerEmail,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PlacedAt,
    decimal Total,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderItemDto(
    Guid Id,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
