using Domain.Common;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Order : Entity
{
    private readonly List<OrderItem> _items = new();

    public string CustomerEmail { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PlacedAt { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public Money Total =>
        _items.Aggregate(
            Money.Zero(Currency),
            (sum, item) => sum.Add(item.LineTotal));

    private Order() { }

    public static Order CreateDraft(string customerEmail, string currency)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new DomainException("Customer email is required.");

        if (!customerEmail.Contains('@'))
            throw new DomainException("Customer email is invalid.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code.");

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerEmail = customerEmail.Trim().ToLowerInvariant(),
            Currency = currency.ToUpperInvariant(),
            Status = OrderStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddItem(string productName, int quantity, Money unitPrice)
    {
        EnsureDraft();

        if (unitPrice.Currency != Currency)
            throw new DomainException(
                $"Item currency {unitPrice.Currency} does not match order currency {Currency}.");

        _items.Add(OrderItem.Create(productName, quantity, unitPrice));
    }

    public void RemoveItem(Guid itemId)
    {
        EnsureDraft();

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException($"Item {itemId} not found in order.");

        _items.Remove(item);
    }

    public void ChangeItemQuantity(Guid itemId, int newQuantity)
    {
        EnsureDraft();

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException($"Item {itemId} not found in order.");

        item.ChangeQuantity(newQuantity);
    }

    public void Place()
    {
        EnsureDraft();

        if (_items.Count == 0)
            throw new DomainException("Cannot place an order with no items.");

        Status = OrderStatus.Placed;
        PlacedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new OrderPlacedEvent(
            Id,
            CustomerEmail,
            Total.Amount,
            Currency));
    }

    public void Fulfill()
    {
        if (Status != OrderStatus.Placed)
            throw new DomainException("Only placed orders can be fulfilled.");

        Status = OrderStatus.Fulfilled;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Fulfilled)
            throw new DomainException("Fulfilled orders cannot be cancelled.");

        Status = OrderStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException(
                $"This operation is only allowed on draft orders. Current status: {Status}.");
    }
}
