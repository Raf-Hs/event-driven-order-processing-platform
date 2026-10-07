namespace Commerce.Domain.Orders;

/// <summary>
/// The outcome of a lifecycle call together with the state the order was in before and is in
/// afterwards, so a caller (consumer, saga, later phases) can log and decide what to publish without
/// re-inspecting the aggregate.
/// </summary>
/// <param name="Outcome">What happened to the request.</param>
/// <param name="StatusBefore">The status the order had when the result was applied.</param>
/// <param name="StatusAfter">The status the order has now. For every outcome other than
/// <see cref="OrderTransitionOutcome.Applied"/> this equals <paramref name="StatusBefore"/>.</param>
public readonly record struct OrderTransitionResult(
    OrderTransitionOutcome Outcome,
    OrderStatus StatusBefore,
    OrderStatus StatusAfter)
{
    /// <summary>True when the call advanced the order's state.</summary>
    public bool IsApplied => Outcome == OrderTransitionOutcome.Applied;

    /// <summary>Builds the result of an applied transition.</summary>
    internal static OrderTransitionResult Applied(OrderStatus from, OrderStatus to)
        => new(OrderTransitionOutcome.Applied, from, to);

    /// <summary>Builds the result of a call that changed nothing.</summary>
    internal static OrderTransitionResult Unchanged(OrderTransitionOutcome outcome, OrderStatus current)
        => new(outcome, current, current);
}
