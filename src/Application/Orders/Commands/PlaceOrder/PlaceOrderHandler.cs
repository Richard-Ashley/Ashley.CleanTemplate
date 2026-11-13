using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Commands.PlaceOrder;

public sealed class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand>
{
    private readonly IApplicationDbContext _db;

    public PlaceOrderHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Order), request.OrderId);

        order.Place();

        await _db.SaveChangesAsync(cancellationToken);
    }
}
