using Application.Orders.Queries.ListOrders;
using Application.Tests.Common;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Application.Tests.Orders.Queries.ListOrders;

public class ListOrdersHandlerTests
{
    [Fact]
    public async Task Handle_WithNoOrders_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new ListOrdersHandler(db);

        var result = await handler.Handle(new ListOrdersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithOrders_ReturnsAllAsSummaries()
    {
        using var db = TestDbContextFactory.Create();

        var order1 = Order.CreateDraft("a@x.com", "USD");
        order1.AddItem("Widget", 2, Money.Create(10m, "USD"));
        db.Orders.Add(order1);

        var order2 = Order.CreateDraft("b@x.com", "EUR");
        order2.AddItem("Gadget", 1, Money.Create(50m, "EUR"));
        order2.AddItem("Thing", 3, Money.Create(5m, "EUR"));
        db.Orders.Add(order2);

        await db.SaveChangesAsync();

        var handler = new ListOrdersHandler(db);

        var result = await handler.Handle(new ListOrdersQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(o => o.CustomerEmail == "a@x.com" && o.Total == 20m && o.ItemCount == 1);
        result.Should().ContainSingle(o => o.CustomerEmail == "b@x.com" && o.Total == 65m && o.ItemCount == 2);
    }

    [Fact]
    public async Task Handle_ReturnsOrdersNewestFirst()
    {
        using var db = TestDbContextFactory.Create();

        var older = Order.CreateDraft("old@x.com", "USD");
        older.AddItem("A", 1, Money.Create(1m, "USD"));
        db.Orders.Add(older);
        await db.SaveChangesAsync();

        await Task.Delay(10);

        var newer = Order.CreateDraft("new@x.com", "USD");
        newer.AddItem("B", 1, Money.Create(1m, "USD"));
        db.Orders.Add(newer);
        await db.SaveChangesAsync();

        var handler = new ListOrdersHandler(db);

        var result = await handler.Handle(new ListOrdersQuery(), CancellationToken.None);

        result[0].CustomerEmail.Should().Be("new@x.com");
        result[1].CustomerEmail.Should().Be("old@x.com");
    }
}
