using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmountAndCurrency_ReturnsMoney()
    {
        var money = Money.Create(100m, "usd");

        money.Amount.Should().Be(100m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_NormalizesCurrencyToUppercase()
    {
        var money = Money.Create(50m, "eur");

        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        var act = () => Money.Create(-1m, "USD");

        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCurrency_Throws(string? currency)
    {
        var act = () => Money.Create(10m, currency!);

        act.Should().Throw<DomainException>()
            .WithMessage("*required*");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("A")]
    public void Create_WithWrongCurrencyLength_Throws(string currency)
    {
        var act = () => Money.Create(10m, currency);

        act.Should().Throw<DomainException>()
            .WithMessage("*3-letter*");
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsSum()
    {
        var a = Money.Create(10m, "USD");
        var b = Money.Create(25m, "USD");

        var result = a.Add(b);

        result.Amount.Should().Be(35m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_WithDifferentCurrencies_Throws()
    {
        var usd = Money.Create(10m, "USD");
        var eur = Money.Create(10m, "EUR");

        var act = () => usd.Add(eur);

        act.Should().Throw<DomainException>()
            .WithMessage("*USD*EUR*");
    }

    [Fact]
    public void Add_DoesNotMutateOriginals()
    {
        var a = Money.Create(10m, "USD");
        var b = Money.Create(5m, "USD");

        _ = a.Add(b);

        a.Amount.Should().Be(10m);
        b.Amount.Should().Be(5m);
    }

    [Fact]
    public void Multiply_ByPositiveQuantity_ReturnsProduct()
    {
        var money = Money.Create(15m, "USD");

        var result = money.Multiply(3);

        result.Amount.Should().Be(45m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Multiply_ByNegativeQuantity_Throws()
    {
        var money = Money.Create(10m, "USD");

        var act = () => money.Multiply(-1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = Money.Create(100m, "USD");
        var b = Money.Create(100m, "USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCurrency_AreNotEqual()
    {
        var usd = Money.Create(100m, "USD");
        var eur = Money.Create(100m, "EUR");

        usd.Should().NotBe(eur);
    }
}
