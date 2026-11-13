using Application.Orders.Commands.CreateOrder;
using Application.Tests.Common;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.Orders.Commands.CreateOrder;

public class CreateOrderHandlerTests
{
    [Fact]
    public async Task Handle_WithValidInput_CreatesDraftOrderWithItems()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateOrderHandler(db);

        var command = new CreateOrderCommand(
            CustomerEmail: "customer@example.com",
            Currency: "USD",
            Items: new List<CreateOrderItemDto>
            {
                new("Widget", 2, 10m),
                new("Gadget", 1, 25m)
            });

        var orderId = await handler.Handle(command, CancellationToken.None);

        orderId.Should().NotBeEmpty();

        var saved = await db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == orderId);
        saved.Status.Should().Be(OrderStatus.Draft);
        saved.CustomerEmail.Should().Be("customer@example.com");
        saved.Currency.Should().Be("USD");
        saved.Items.Should().HaveCount(2);
        saved.Total.Amount.Should().Be(45m);
    }

    [Fact]
    public async Task Handle_PersistsOrderToDatabase()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateOrderHandler(db);

        var command = new CreateOrderCommand(
            "x@y.com",
            "EUR",
            new List<CreateOrderItemDto> { new("Item", 1, 5m) });

        await handler.Handle(command, CancellationToken.None);

        var count = await db.Orders.CountAsync();
        count.Should().Be(1);
    }
}
