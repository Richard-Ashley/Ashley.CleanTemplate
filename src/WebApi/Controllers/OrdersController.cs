using Application.Orders.Commands.CreateOrder;
using Application.Orders.Commands.PlaceOrder;
using Application.Orders.Queries.GetOrderById;
using Application.Orders.Queries.ListOrders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Manages customer orders.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns all orders, newest first.
    /// </summary>
    /// <response code="200">A list of order summaries.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var orders = await _mediator.Send(new ListOrdersQuery(), cancellationToken);
        return Ok(orders);
    }

    /// <summary>
    /// Returns the order with the specified identifier.
    /// </summary>
    /// <param name="id">The order's unique identifier.</param>
    /// <response code="200">The requested order.</response>
    /// <response code="404">No order with that identifier exists.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return Ok(order);
    }

    /// <summary>
    /// Creates a new draft order with the given items.
    /// </summary>
    /// <param name="command">The order to create.</param>
    /// <response code="201">The order was created. The response body contains the new order's identifier.</response>
    /// <response code="400">The request failed validation or violated a domain rule.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = orderId }, orderId);
    }

    /// <summary>
    /// Transitions a draft order to the Placed status.
    /// </summary>
    /// <param name="id">The order's unique identifier.</param>
    /// <response code="204">The order was placed.</response>
    /// <response code="404">No order with that identifier exists.</response>
    /// <response code="400">The order cannot be placed (not in Draft status, or has no items).</response>
    [HttpPost("{id:guid}/place")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Place(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new PlaceOrderCommand(id), cancellationToken);
        return NoContent();
    }
}
