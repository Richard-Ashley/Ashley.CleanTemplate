using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IApplicationDbContext _db;

    public GetOrderByIdHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Order), request.OrderId);

        return new OrderDto(
            order.Id,
            order.CustomerEmail,
            order.Currency,
            order.Status.ToString(),
            order.CreatedAt,
            order.PlacedAt,
            order.Total.Amount,
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.ProductName,
                i.Quantity,
                i.UnitPrice.Amount,
                i.LineTotal.Amount)).ToList());
    }
}
