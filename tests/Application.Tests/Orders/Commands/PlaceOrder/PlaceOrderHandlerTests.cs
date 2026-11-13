using Application.Common.Exceptions;
using Application.Orders.Commands.PlaceOrder;
using Application.Tests.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.ValueObjects;
using FluentAssertions;

namespace Application.Tests.Orders.Commands.PlaceOrder;

public class PlaceOrderHandlerTests
{
    [Fact]
    public async Task Handle_WithDraftOrderWithItems_PlacesOrder()
    {
        using var db = TestDbContextFactory.Create();
        var order = Order.CreateDraft("x@y.com", "USD");
        order.AddItem("Widget", 1, Money.Create(10m, "USD"));
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new PlaceOrderHandler(db);

        await handler.Handle(new PlaceOrderCommand(order.Id), CancellationToken.None);

        var reloaded = await db.Orders.FindAsync(order.Id);
        reloaded!.Status.Should().Be(OrderStatus.Placed);
        reloaded.PlacedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_RaisesOrderPlacedEventOnDomainEntity()
    {
        using var db = TestDbContextFactory.Create();
        var order = Order.CreateDraft("x@y.com", "USD");
        order.AddItem("Widget", 1, Money.Create(10m, "USD"));
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new PlaceOrderHandler(db);
        await handler.Handle(new PlaceOrderCommand(order.Id), CancellationToken.None);

        var reloaded = await db.Orders.FindAsync(order.Id);
        reloaded!.DomainEvents.Should().ContainSingle(e => e is OrderPlacedEvent);
    }

    [Fact]
    public async Task Handle_WithMissingOrder_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new PlaceOrderHandler(db);

        var act = async () =>
            await handler.Handle(new PlaceOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
