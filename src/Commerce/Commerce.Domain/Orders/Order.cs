using System.Collections.ObjectModel;

namespace Commerce.Domain.Orders;

/// <summary>
/// The Commerce aggregate root (PLAN.md §8): a customer's submitted order with its immutable line
/// snapshots, a derived total, its lifecycle status, and the workflow attempt whose results are
/// authoritative for it.
/// <para>
/// The aggregate owns its state transitions. Callers may only report workflow <em>results</em>
/// (<see cref="ApplyInventoryReserved"/>, <see cref="ApplyInventoryRejected"/>,
/// <see cref="ApplyPaymentAuthorized"/>, <see cref="ApplyPaymentDeclined"/>); there is no way to set
/// <see cref="Status"/>, <see cref="Total"/>, or a line from the outside, because neither clients
/// nor other services may hold authoritative order state (PLAN.md §6, §8).
/// </para>
/// <para>
/// Every transition is guarded by the <em>attempt number of the incoming result</em> and the current
/// status, because delivery is at-least-once: duplicates, redeliveries, and results from a
/// superseded attempt are expected and must be recognized as no-ops instead of mutations
/// (PLAN.md §9, §18). A guarded call returns an <see cref="OrderTransitionResult"/> and does not
/// throw for those expected conditions.
/// </para>
/// <para>
/// Out of scope by design at this stage: message envelopes and outbox/inbox writes, saga step
/// selection, retries, timeouts, compensation (<c>ReleaseInventory</c>), failure reason codes,
/// status history, and persistence timestamps/concurrency tokens. See
/// <c>docs/architecture/order-domain.md</c>.
/// </para>
/// </summary>
public sealed class Order
{
    /// <summary>The attempt number a freshly submitted order is authoritative for.</summary>
    public const int InitialAttempt = 1;

    private Order(
        OrderId orderId,
        CustomerId customerId,
        OrderLineSnapshot[] lines,
        Money total,
        OrderStatus status,
        int currentAttempt)
    {
        // The identifiers are value types, and a value type's default exists without running its
        // constructor, so Submit/Rehydrate can be handed default(OrderId) or default(CustomerId).
        // This is the boundary that makes the aggregate, so it rechecks them (PLAN.md §8: value
        // objects validate at construction/application boundaries).
        if (!orderId.IsValid)
        {
            throw new ArgumentException("An order id must be a non-empty identifier.", nameof(orderId));
        }

        if (!customerId.IsValid)
        {
            throw new ArgumentException("A customer id must be a non-empty identifier.", nameof(customerId));
        }

        // Total is the one piece of state this aggregate derives yet receives as a parameter, and an
        // order without valid money can never be authorized or failed correctly. Both factories pass
        // a total computed from validated lines; this asserts that the invariant holds here rather
        // than relying on the call sites two methods away.
        if (!total.IsValid)
        {
            throw new ArgumentException(
                "An order total must be a valid money amount: a nonnegative minor-unit value and a three-letter currency.",
                nameof(total));
        }

        Id = orderId;
        CustomerId = customerId;
        // Copy already happened in ValidateLines; wrapping it keeps the exposed view read-only and
        // not downcastable to the backing array.
        Lines = new ReadOnlyCollection<OrderLineSnapshot>(lines);
        Total = total;
        Status = status;
        CurrentAttempt = currentAttempt;
    }

    /// <summary>Stable order identifier.</summary>
    public OrderId Id { get; }

    /// <summary>Customer who submitted the order; ownership is enforced at the API boundary.</summary>
    public CustomerId CustomerId { get; }

    /// <summary>
    /// Immutable line snapshots exactly as sold. The caller's collection is copied at construction
    /// and the exposed view is read-only, so neither the submitting code path nor a later catalog
    /// change can alter an accepted order (PLAN.md §8, §14 "immutable line snapshots").
    /// </summary>
    public IReadOnlyList<OrderLineSnapshot> Lines { get; }

    /// <summary>
    /// Sum of the line totals, derived with checked arithmetic and always in the order's single
    /// currency. Never supplied by a caller (PLAN.md §6: clients cannot set authoritative totals).
    /// </summary>
    public Money Total { get; }

    /// <summary>Current lifecycle status. Only the transition methods can change it.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// The workflow attempt whose results this order accepts. A result carrying any other attempt
    /// number is ignored (PLAN.md §9: order/attempt/version checks prevent stale messages changing
    /// current state). Opening a new attempt is orchestration owned by the saga, so this aggregate
    /// only ever reads the value.
    /// </summary>
    public int CurrentAttempt { get; }

    /// <summary>
    /// True when no further MVP transition can apply: <see cref="OrderStatus.Confirmed"/> and
    /// <see cref="OrderStatus.Failed"/> are terminal for the initial workflow (PLAN.md §9).
    /// </summary>
    public bool IsTerminal => Status is OrderStatus.Confirmed or OrderStatus.Failed;

    /// <summary>
    /// Creates a newly submitted order in <see cref="OrderStatus.PendingInventory"/> at
    /// <see cref="InitialAttempt"/>. The order's currency and total come from the lines.
    /// </summary>
    /// <param name="orderId">Stable order identifier; must be a valid (non-empty) identifier.</param>
    /// <param name="customerId">Submitting customer; must be a valid (non-empty) identifier.</param>
    /// <param name="lines">At least one valid line snapshot, all in one currency; copied, not retained.</param>
    /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="orderId"/> or
    /// <paramref name="customerId"/> is a default identifier, there are no lines, a line is a
    /// default or otherwise invalid snapshot, or more than one currency is present.</exception>
    /// <exception cref="OverflowException">The derived total exceeds the range of <see cref="long"/> minor units.</exception>
    public static Order Submit(OrderId orderId, CustomerId customerId, IReadOnlyList<OrderLineSnapshot> lines)
    {
        var snapshot = ValidateLines(lines);
        return new Order(orderId, customerId, snapshot, TotalOf(snapshot), OrderStatus.PendingInventory, InitialAttempt);
    }

    /// <summary>
    /// Rebuilds the aggregate from state the owning service already holds, performing no transition.
    /// This is the construction seam the persistence layer uses and the seam that lets the guards be
    /// exercised against a superseded attempt. It adds no lifecycle capability and cannot smuggle in
    /// an inconsistent aggregate: the identifiers are rechecked, the lines are re-validated, the
    /// total is recomputed from them (so a stored total can never disagree with the lines it claims
    /// to sum), and the status and attempt must be valid.
    /// </summary>
    /// <param name="orderId">Stable order identifier; must be a valid (non-empty) identifier.</param>
    /// <param name="customerId">Submitting customer; must be a valid (non-empty) identifier.</param>
    /// <param name="lines">At least one valid line snapshot, all in one currency.</param>
    /// <param name="status">A defined <see cref="OrderStatus"/>.</param>
    /// <param name="currentAttempt">The attempt whose results are authoritative for this order.</param>
    /// <exception cref="ArgumentException"><paramref name="status"/> is not a defined
    /// <see cref="OrderStatus"/>, <paramref name="orderId"/> or <paramref name="customerId"/> is a
    /// default identifier, or the lines fail validation.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="currentAttempt"/> is below
    /// <see cref="InitialAttempt"/>.</exception>
    /// <exception cref="OverflowException">The recomputed total exceeds the range of <see cref="long"/> minor units.</exception>
    public static Order Rehydrate(
        OrderId orderId,
        CustomerId customerId,
        IReadOnlyList<OrderLineSnapshot> lines,
        OrderStatus status,
        int currentAttempt)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException($"'{status}' is not a defined order status.", nameof(status));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(currentAttempt, InitialAttempt);

        var snapshot = ValidateLines(lines);
        return new Order(orderId, customerId, snapshot, TotalOf(snapshot), status, currentAttempt);
    }

    /// <summary>
    /// Applies an <c>InventoryReserved</c> result: PendingInventory → PendingPayment. Any other
    /// current state, or a result from another attempt, changes nothing.
    /// </summary>
    /// <param name="attempt">The attempt number the reservation result belongs to.</param>
    public OrderTransitionResult ApplyInventoryReserved(int attempt)
    {
        if (Guard(attempt, OrderStatus.PendingInventory, OrderStatus.PendingPayment) is { } ignored)
        {
            return ignored;
        }

        return Advance(OrderStatus.PendingPayment);
    }

    /// <summary>
    /// Applies an <c>InventoryRejected</c> result: PendingInventory → Failed. A repeated rejection is
    /// a no-op, and a rejection belonging to a superseded attempt must never fail an order that has
    /// already moved on.
    /// </summary>
    /// <param name="attempt">The attempt number the rejection result belongs to.</param>
    public OrderTransitionResult ApplyInventoryRejected(int attempt)
    {
        if (Guard(attempt, OrderStatus.PendingInventory, OrderStatus.Failed) is { } ignored)
        {
            return ignored;
        }

        return Advance(OrderStatus.Failed);
    }

    /// <summary>
    /// Applies a <c>PaymentAuthorized</c> result: PendingPayment → Confirmed, and only when the
    /// authorized amount and currency equal <see cref="Total"/> (PLAN.md §9: "Must match order,
    /// attempt, amount, currency, and current state"). A mismatch confirms nothing and changes
    /// nothing; the order stays PendingPayment so the caller can reconcile or fail it deliberately.
    /// </summary>
    /// <param name="attempt">The attempt number the authorization belongs to.</param>
    /// <param name="authorizedAmount">Amount and currency reported as authorized.</param>
    public OrderTransitionResult ApplyPaymentAuthorized(int attempt, Money authorizedAmount)
    {
        if (Guard(attempt, OrderStatus.PendingPayment, OrderStatus.Confirmed) is { } ignored)
        {
            return ignored;
        }

        if (authorizedAmount != Total)
        {
            return OrderTransitionResult.Unchanged(OrderTransitionOutcome.RejectedPaymentMismatch, Status);
        }

        return Advance(OrderStatus.Confirmed);
    }

    /// <summary>
    /// Applies a <c>PaymentDeclined</c> result: PendingPayment → Failed. Issuing the
    /// <c>ReleaseInventory</c> compensation and recording a failure reason are orchestration concerns
    /// and are intentionally not modelled here.
    /// </summary>
    /// <param name="attempt">The attempt number the decline belongs to.</param>
    public OrderTransitionResult ApplyPaymentDeclined(int attempt)
    {
        if (Guard(attempt, OrderStatus.PendingPayment, OrderStatus.Failed) is { } ignored)
        {
            return ignored;
        }

        return Advance(OrderStatus.Failed);
    }

    /// <summary>
    /// The single guard shared by all four transitions: returns the no-op result to hand back, or
    /// <see langword="null"/> when the result is authoritative for this order right now. Attempt
    /// membership is checked before status so a result from a different attempt can never be
    /// classified — or acted on — as though it described the current workflow.
    /// </summary>
    private OrderTransitionResult? Guard(int attempt, OrderStatus expectedFrom, OrderStatus target)
    {
        if (attempt < CurrentAttempt)
        {
            return OrderTransitionResult.Unchanged(OrderTransitionOutcome.IgnoredStaleAttempt, Status);
        }

        if (attempt > CurrentAttempt)
        {
            return OrderTransitionResult.Unchanged(OrderTransitionOutcome.IgnoredFutureAttempt, Status);
        }

        // Same outcome already recorded: a redelivery under at-least-once semantics.
        if (Status == target)
        {
            return OrderTransitionResult.Unchanged(OrderTransitionOutcome.IgnoredDuplicate, Status);
        }

        // Arrived too early, or would regress a terminal order.
        if (Status != expectedFrom)
        {
            return OrderTransitionResult.Unchanged(OrderTransitionOutcome.IgnoredOutOfOrder, Status);
        }

        return null;
    }

    private OrderTransitionResult Advance(OrderStatus target)
    {
        var previous = Status;
        Status = target;
        return OrderTransitionResult.Applied(previous, target);
    }

    /// <summary>
    /// Copies the caller's lines and enforces the order-level invariants before anything is summed:
    /// at least one line, every line a valid snapshot, and one currency across the whole order
    /// (PLAN.md §8). Per-line rules are also enforced by the line and value types' constructors, but
    /// a value type's <c>default</c> value never runs a constructor, so this aggregate rechecks each
    /// line itself — otherwise <c>default(OrderLineSnapshot)</c> would contribute a null-currency
    /// amount, agree with itself on currency, and produce an order whose total can never be
    /// authorized.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">There are no lines, a line is not a valid snapshot, or
    /// more than one currency is present.</exception>
    private static OrderLineSnapshot[] ValidateLines(IReadOnlyList<OrderLineSnapshot> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
        {
            throw new ArgumentException(
                "An order must contain at least one line; its total and currency are undefined without one.",
                nameof(lines));
        }

        var snapshot = new OrderLineSnapshot[lines.Count];
        for (var index = 0; index < snapshot.Length; index++)
        {
            var line = lines[index];
            if (!line.IsValid)
            {
                // Deliberately reports the position only: an invalid snapshot's SKU and currency may
                // be null, so they cannot be used to build the message.
                throw new ArgumentException(
                    $"Line {index} is not a valid order line snapshot: a non-empty SKU, a non-blank display name, a positive quantity, and a unit amount with a three-letter currency are required.",
                    nameof(lines));
            }

            snapshot[index] = line;
        }

        var currency = snapshot[0].UnitAmount.Currency;
        foreach (var line in snapshot)
        {
            if (!string.Equals(line.UnitAmount.Currency, currency, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"An order uses a single currency: line '{line.Sku}' is {line.UnitAmount.Currency} but the order is {currency}.",
                    nameof(lines));
            }
        }

        return snapshot;
    }

    private static Money TotalOf(OrderLineSnapshot[] lines)
    {
        var total = lines[0].LineTotal;
        for (var index = 1; index < lines.Length; index++)
        {
            total = total.Add(lines[index].LineTotal);
        }

        return total;
    }
}
