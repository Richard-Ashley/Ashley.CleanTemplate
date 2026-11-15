using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Queries.ListOrders;

public sealed class ListOrdersHandler : IRequestHandler<ListOrdersQuery, IReadOnlyList<OrderSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public ListOrdersHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.CustomerEmail,
                o.Status.ToString(),
                o.CreatedAt,
                o.TotalAmount,
                o.Currency,
                o.Items.Count))
            .ToListAsync(cancellationToken);
    }
}
