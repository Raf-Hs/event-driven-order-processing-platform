using Commerce.Domain.Orders;

namespace Commerce.UnitTests.Orders;

/// <summary>
/// Deterministic builders for the Commerce order domain tests. Identifiers are fixed values rather
/// than <see cref="Guid.NewGuid"/> so every test is reproducible and failures are readable.
/// </summary>
internal static class TestOrders
{
    internal static readonly OrderId OrderId = new(new Guid("11111111-1111-1111-1111-111111111111"));
    internal static readonly CustomerId CustomerId = new(new Guid("22222222-2222-2222-2222-222222222222"));

    /// <summary>A single sellable line: 1 unit at 1000 minor units USD unless overridden.</summary>
    internal static OrderLineSnapshot Line(
        string sku = "SKU-1",
        int quantity = 1,
        long unitAmountMinor = 1_000,
        string currency = "USD")
        => new(new Sku(sku), $"{sku} test product", new Quantity(quantity), new Money(unitAmountMinor, currency));

    /// <summary>Two lines of the same currency: 2 × 1000 and 1 × 250 → 2250 minor units.</summary>
    internal static IReadOnlyList<OrderLineSnapshot> TwoLines()
        => [Line("SKU-1", quantity: 2, unitAmountMinor: 1_000), Line("SKU-2", quantity: 1, unitAmountMinor: 250)];

    /// <summary>One default line as a list, for the common single-line order.</summary>
    internal static IReadOnlyList<OrderLineSnapshot> OneLine() => [Line()];

    /// <summary>An order rebuilt in a chosen state/attempt, for transition and guard tests.</summary>
    internal static Order InState(
        OrderStatus status,
        int attempt = Order.InitialAttempt,
        IReadOnlyList<OrderLineSnapshot>? lines = null)
        => Order.Rehydrate(OrderId, CustomerId, lines ?? OneLine(), status, attempt);

    /// <summary>A freshly submitted single-line order (PendingInventory, attempt 1).</summary>
    internal static Order Submitted() => Order.Submit(OrderId, CustomerId, OneLine());
}
