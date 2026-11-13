using Application.Common.Exceptions;
using Application.Orders.Queries.GetOrderById;
using Application.Tests.Common;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Application.Tests.Orders.Queries.GetOrderById;

public class GetOrderByIdHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingOrder_ReturnsOrderDto()
    {
        using var db = TestDbContextFactory.Create();
        var order = Order.CreateDraft("customer@example.com", "USD");
        order.AddItem("Widget", 2, Money.Create(10m, "USD"));
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new GetOrderByIdHandler(db);

        var dto = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        dto.Id.Should().Be(order.Id);
        dto.CustomerEmail.Should().Be("customer@example.com");
        dto.Status.Should().Be("Draft");
        dto.Total.Should().Be(20m);
        dto.Items.Should().HaveCount(1);
        dto.Items[0].LineTotal.Should().Be(20m);
    }

    [Fact]
    public async Task Handle_WithMissingOrder_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetOrderByIdHandler(db);

        var act = async () =>
            await handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
