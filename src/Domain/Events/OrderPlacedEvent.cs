using Domain.Common;

namespace Domain.Events;

public sealed record OrderPlacedEvent(
    Guid OrderId,
    string CustomerEmail,
    decimal TotalAmount,
    string Currency) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
