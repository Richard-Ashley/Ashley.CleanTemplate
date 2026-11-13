using MediatR;

namespace Application.Orders.Commands.PlaceOrder;

public sealed record PlaceOrderCommand(Guid OrderId) : IRequest;
