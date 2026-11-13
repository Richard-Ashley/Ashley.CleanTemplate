using Application.Orders.Commands.CreateOrder;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Application.Tests.Orders.Commands.CreateOrder;

public class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_Succeeds()
    {
        var command = new CreateOrderCommand(
            "customer@example.com",
            "USD",
            new List<CreateOrderItemDto> { new("Widget", 1, 10m) });

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@missing.com")]
    public void Validate_WithInvalidEmail_HasError(string email)
    {
        var command = new CreateOrderCommand(
            email,
            "USD",
            new List<CreateOrderItemDto> { new("Widget", 1, 10m) });

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Validate_WithInvalidCurrency_HasError(string currency)
    {
        var command = new CreateOrderCommand(
            "x@y.com",
            currency,
            new List<CreateOrderItemDto> { new("Widget", 1, 10m) });

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void Validate_WithEmptyItems_HasError()
    {
        var command = new CreateOrderCommand("x@y.com", "USD", new List<CreateOrderItemDto>());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_WithItemWithZeroQuantity_HasError()
    {
        var command = new CreateOrderCommand(
            "x@y.com",
            "USD",
            new List<CreateOrderItemDto> { new("Widget", 0, 10m) });

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void Validate_WithItemWithNegativePrice_HasError()
    {
        var command = new CreateOrderCommand(
            "x@y.com",
            "USD",
            new List<CreateOrderItemDto> { new("Widget", 1, -5m) });

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice");
    }
}
