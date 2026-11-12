using Domain.Common;
using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");

        if (currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code.");

        return new Money(amount, currency.ToUpperInvariant());
    }

    public static Money Zero(string currency) => Create(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot add {Currency} to {other.Currency}.");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("Quantity cannot be negative.");

        return new Money(Amount * quantity, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
