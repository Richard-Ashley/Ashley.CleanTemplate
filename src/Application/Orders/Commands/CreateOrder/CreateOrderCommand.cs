using MediatR;

namespace Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    string CustomerEmail,
    string Currency,
    IReadOnlyList<CreateOrderItemDto> Items) : IRequest<Guid>;

public sealed record CreateOrderItemDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice);
