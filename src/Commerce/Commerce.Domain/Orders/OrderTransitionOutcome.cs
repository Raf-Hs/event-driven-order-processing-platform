namespace Commerce.Domain.Orders;

/// <summary>
/// Outcome of applying a workflow result to an <see cref="Order"/> (PLAN.md §9). Delivery is
/// at-least-once, so results legitimately arrive more than once and out of order; only
/// <see cref="Applied"/> mutates the aggregate. Every other outcome is an explicit no-op that the
/// caller (consumer/saga, later phases) records and acknowledges rather than retrying as an
/// exception.
/// </summary>
/// <remarks>
/// This is the domain's small result convention for lifecycle outcomes. Construction-time invariant
/// violations are separate and fail fast with standard argument exceptions
/// (<see cref="ArgumentNullException"/>, <see cref="ArgumentException"/>,
/// <see cref="ArgumentOutOfRangeException"/>) or <see cref="OverflowException"/> from checked
/// arithmetic; a malformed workflow result is never an exception, because ignoring it is safe and
/// correct.
/// </remarks>
public enum OrderTransitionOutcome
{
    /// <summary>The result matched the current attempt and expected state, so the state advanced.</summary>
    Applied,

    /// <summary>
    /// The order already holds the outcome this result reports (redelivery of a result already
    /// applied). No change.
    /// </summary>
    IgnoredDuplicate,

    /// <summary>
    /// The result does not belong to the order's current state — it arrived too early, or reports an
    /// outcome that would regress a terminal order. No change (PLAN.md §9: "do not regress state").
    /// </summary>
    IgnoredOutOfOrder,

    /// <summary>
    /// The result belongs to an older attempt that the workflow has already superseded. No change
    /// (PLAN.md §9: order/attempt/version checks prevent stale messages changing current state).
    /// </summary>
    IgnoredStaleAttempt,

    /// <summary>
    /// The result claims an attempt newer than the order knows about, which means the data is
    /// inconsistent with this aggregate. No change; the caller should surface it for
    /// reconciliation/alerting rather than act on it.
    /// </summary>
    IgnoredFutureAttempt,

    /// <summary>
    /// A payment authorization result whose amount or currency does not match the order total.
    /// The order is not confirmed and does not change (PLAN.md §9: "Must match order, attempt,
    /// amount, currency, and current state").
    /// </summary>
    RejectedPaymentMismatch,
}
