using Application.Orders.Queries.GetOrderById;
using MediatR;

namespace Application.Orders.Queries.ListOrders;

public sealed record ListOrdersQuery() : IRequest<IReadOnlyList<OrderSummaryDto>>;

public sealed record OrderSummaryDto(
    Guid Id,
    string CustomerEmail,
    string Status,
    DateTimeOffset CreatedAt,
    decimal Total,
    string Currency,
    int ItemCount);
