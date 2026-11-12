using Domain.Common;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class OrderItem : Entity
{
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderItem() { }

    internal static OrderItem Create(string productName, int quantity, Money unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductName = productName.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    internal void ChangeQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        Quantity = newQuantity;
    }
}
