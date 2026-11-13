using Application.Common.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;

namespace Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateOrderHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = Order.CreateDraft(request.CustomerEmail, request.Currency);

        foreach (var item in request.Items)
        {
            var unitPrice = Money.Create(item.UnitPrice, request.Currency);
            order.AddItem(item.ProductName, item.Quantity, unitPrice);
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
