using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities;

public class OrderTests
{
    private static Money Usd(decimal amount) => Money.Create(amount, "USD");

    [Fact]
    public void CreateDraft_WithValidInput_ReturnsDraftOrder()
    {
        var order = Order.CreateDraft("customer@example.com", "usd");

        order.Status.Should().Be(OrderStatus.Draft);
        order.CustomerEmail.Should().Be("customer@example.com");
        order.Currency.Should().Be("USD");
        order.Items.Should().BeEmpty();
        order.PlacedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void CreateDraft_WithInvalidEmail_Throws(string? email)
    {
        var act = () => Order.CreateDraft(email!, "USD");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateDraft_WithMalformedEmailThatContainsAt_StillCreatesOrder()
    {
        // Documents an intentional asymmetry: the Domain only checks for '@',
        // while the Application-layer FluentValidation uses full RFC-style EmailAddress validation.
        // The Domain's check is defense-in-depth for code paths that bypass the pipeline
        // (background jobs, direct handler calls, tests). The stricter format check lives at the API boundary.

        var order = Order.CreateDraft("not-really-an-email@butstillpasses", "USD");

        order.Should().NotBeNull();
        order.CustomerEmail.Should().Be("not-really-an-email@butstillpasses");
    }

    [Fact]
    public void AddItem_ToDraft_AddsToItems()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");

        order.AddItem("Widget", 2, Usd(10m));

        order.Items.Should().HaveCount(1);
        order.Items[0].ProductName.Should().Be("Widget");
        order.Items[0].Quantity.Should().Be(2);
        order.Items[0].LineTotal.Amount.Should().Be(20m);
    }

    [Fact]
    public void AddItem_WithDifferentCurrency_Throws()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");

        var act = () => order.AddItem("Widget", 1, Money.Create(10m, "EUR"));

        act.Should().Throw<DomainException>()
            .WithMessage("*EUR*USD*");
    }

    [Fact]
    public void Total_WithMultipleItems_SumsLineTotals()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");
        order.AddItem("Widget", 2, Usd(10m));
        order.AddItem("Gadget", 3, Usd(5m));

        order.Total.Amount.Should().Be(35m);
        order.Total.Currency.Should().Be("USD");
    }

    [Fact]
    public void TotalAmount_IsRecalculatedOnEveryItemChange()
    {
        var order = Order.CreateDraft("x@y.com", "USD");

        order.TotalAmount.Should().Be(0m);

        order.AddItem("Widget", 2, Usd(10m));
        order.TotalAmount.Should().Be(20m);

        order.AddItem("Gadget", 1, Usd(5m));
        order.TotalAmount.Should().Be(25m);

        order.ChangeItemQuantity(order.Items[0].Id, 5);
        order.TotalAmount.Should().Be(55m);

        order.RemoveItem(order.Items[1].Id);
        order.TotalAmount.Should().Be(50m);
    }

    [Fact]
    public void Place_WithItems_TransitionsToPlaced()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");
        order.AddItem("Widget", 1, Usd(10m));

        order.Place();

        order.Status.Should().Be(OrderStatus.Placed);
        order.PlacedAt.Should().NotBeNull();
    }

    [Fact]
    public void Place_WithNoItems_Throws()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");

        var act = () => order.Place();

        act.Should().Throw<DomainException>()
            .WithMessage("*no items*");
    }

    [Fact]
    public void Place_RaisesOrderPlacedEvent()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");
        order.AddItem("Widget", 2, Usd(10m));

        order.Place();

        order.DomainEvents.Should().HaveCount(1);
        var evt = order.DomainEvents[0].Should().BeOfType<OrderPlacedEvent>().Subject;
        evt.OrderId.Should().Be(order.Id);
        evt.CustomerEmail.Should().Be("customer@example.com");
        evt.TotalAmount.Should().Be(20m);
        evt.Currency.Should().Be("USD");
    }

    [Fact]
    public void AddItem_AfterPlace_Throws()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");
        order.AddItem("Widget", 1, Usd(10m));
        order.Place();

        var act = () => order.AddItem("Another", 1, Usd(5m));

        act.Should().Throw<DomainException>()
            .WithMessage("*draft*");
    }

    [Fact]
    public void RemoveItem_AfterPlace_Throws()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");
        order.AddItem("Widget", 1, Usd(10m));
        var itemId = order.Items[0].Id;
        order.Place();

        var act = () => order.RemoveItem(itemId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ChangeItemQuantity_OnDraft_UpdatesQuantity()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");
        order.AddItem("Widget", 1, Usd(10m));
        var itemId = order.Items[0].Id;

        order.ChangeItemQuantity(itemId, 5);

        order.Items[0].Quantity.Should().Be(5);
        order.Total.Amount.Should().Be(50m);
    }

    [Fact]
    public void Fulfill_WhenPlaced_TransitionsToFulfilled()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");
        order.AddItem("Widget", 1, Usd(10m));
        order.Place();

        order.Fulfill();

        order.Status.Should().Be(OrderStatus.Fulfilled);
    }

    [Fact]
    public void Fulfill_WhenDraft_Throws()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");

        var act = () => order.Fulfill();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_WhenDraft_TransitionsToCancelled()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenFulfilled_Throws()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");
        order.AddItem("Widget", 1, Usd(10m));
        order.Place();
        order.Fulfill();

        var act = () => order.Cancel();

        act.Should().Throw<DomainException>()
            .WithMessage("*Fulfilled*");
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var order = Order.CreateDraft("customer@example.com", "USD");
        order.AddItem("Widget", 1, Usd(10m));
        order.Place();

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }
}
