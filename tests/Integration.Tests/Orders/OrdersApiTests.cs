using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Orders.Commands.CreateOrder;
using Application.Orders.Queries.GetOrderById;
using Application.Orders.Queries.ListOrders;
using FluentAssertions;

namespace Integration.Tests.Orders;

public sealed class OrdersApiTests : IntegrationTestBase
{
    public OrdersApiTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task List_WhenEmpty_ReturnsEmptyArray()
    {
        var response = await Client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await ReadJson<List<OrderSummaryDto>>(response);
        orders.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Create_WithValidBody_Returns201WithLocationHeader()
    {
        var command = new CreateOrderCommand(
            "customer@example.com",
            "USD",
            new List<CreateOrderItemDto>
            {
                new("Widget", 2, 15.50m)
            });

        var response = await Client.PostAsJsonAsync("/api/orders", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/orders/");

        var orderId = await ReadJson<Guid>(response);
        orderId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_WithInvalidBody_Returns400WithFieldErrors()
    {
        var command = new CreateOrderCommand(
            "not-an-email",
            "US",
            new List<CreateOrderItemDto>());

        var response = await Client.PostAsJsonAsync("/api/orders", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await ReadJson<Dictionary<string, object>>(response);
        problem.Should().NotBeNull();
        problem!.Should().ContainKey("errors");

        var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
            problem["errors"].ToString()!, JsonOptions);

        errors.Should().ContainKey("customerEmail");
        errors.Should().ContainKey("currency");
        errors.Should().ContainKey("items");
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404()
    {
        var response = await Client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOrder()
    {
        var orderId = await CreateOrder();

        var response = await Client.GetAsync($"/api/orders/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await ReadJson<OrderDto>(response);
        order.Should().NotBeNull();
        order!.Id.Should().Be(orderId);
        order.Status.Should().Be("Draft");
        order.Total.Should().Be(31m);
    }

    [Fact]
    public async Task Place_OnDraftWithItems_Returns204()
    {
        var orderId = await CreateOrder();

        var response = await Client.PostAsync($"/api/orders/{orderId}/place", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/orders/{orderId}");
        var order = await ReadJson<OrderDto>(getResponse);
        order!.Status.Should().Be("Placed");
    }

    [Fact]
    public async Task Place_OnAlreadyPlaced_Returns400WithDomainMessage()
    {
        var orderId = await CreateOrder();
        await Client.PostAsync($"/api/orders/{orderId}/place", content: null);

        var response = await Client.PostAsync($"/api/orders/{orderId}/place", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await ReadJson<Dictionary<string, object>>(response);
        problem!["detail"].ToString().Should().Contain("only allowed on draft orders");
    }

    [Fact]
    public async Task Place_OnUnknownId_Returns404()
    {
        var response = await Client.PostAsync($"/api/orders/{Guid.NewGuid()}/place", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_AfterCreatingTwoOrders_ReturnsBothNewestFirst()
    {
        await CreateOrder("first@example.com");
        await Task.Delay(10);
        await CreateOrder("second@example.com");

        var response = await Client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await ReadJson<List<OrderSummaryDto>>(response);
        orders.Should().HaveCount(2);
        orders![0].CustomerEmail.Should().Be("second@example.com");
        orders[1].CustomerEmail.Should().Be("first@example.com");
    }

    private async Task<Guid> CreateOrder(string email = "customer@example.com")
    {
        var command = new CreateOrderCommand(
            email,
            "USD",
            new List<CreateOrderItemDto>
            {
                new("Widget", 2, 15.50m)
            });

        var response = await Client.PostAsJsonAsync("/api/orders", command);
        response.EnsureSuccessStatusCode();
        return (await ReadJson<Guid>(response))!;
    }
}
