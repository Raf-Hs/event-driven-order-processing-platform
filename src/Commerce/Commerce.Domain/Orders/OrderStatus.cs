namespace Commerce.Domain.Orders;

/// <summary>
/// The externally visible lifecycle state of an <see cref="Order"/> (PLAN.md §9).
/// MVP exposes only the four values below; <see cref="Confirmed"/> and <see cref="Failed"/> are
/// terminal for the initial workflow. Processing/Cancelled/Shipped/Completed are deliberately
/// absent — they belong to later scope and must not be fabricated here.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Initial state after submission. The order is durable and waits for the inventory
    /// reservation result. Acceptance is not completion (PLAN.md §9, §12).
    /// </summary>
    PendingInventory = 0,

    /// <summary>
    /// Inventory reserved; the order now waits for the payment authorization result.
    /// </summary>
    PendingPayment = 1,

    /// <summary>
    /// Reservation and payment authorization both succeeded. Terminal.
    /// An order may not reach this state until both have succeeded (PLAN.md §8).
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Permanent unsuccessful outcome from either workflow stage. Terminal for MVP.
    /// </summary>
    Failed = 3,
}
