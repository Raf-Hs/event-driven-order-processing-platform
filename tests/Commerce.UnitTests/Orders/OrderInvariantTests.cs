using Commerce.Domain.Orders;

namespace Commerce.UnitTests.Orders;

/// <summary>
/// Aggregate-boundary invariants for value types (PLAN.md §8: "Value objects validate at
/// construction/application boundaries"). A value type's <c>default</c> value never runs its
/// constructor, so <c>default(OrderId)</c>, <c>default(CustomerId)</c> and
/// <c>default(OrderLineSnapshot)</c> can reach <see cref="Order.Submit"/> and
/// <see cref="Order.Rehydrate"/> fully formed and empty. Before this rule was enforced, a single
/// default line passed the currency comparison (null equals null) and produced an order whose total
/// had no currency at all: permanently unauthorized-able, because
/// <see cref="Order.ApplyPaymentAuthorized"/> can never match a null currency. These are the
/// regression tests for that reviewed defect (M1-01-FIX-1).
/// </summary>
public class OrderInvariantTests
{
    private static readonly OrderLineSnapshot ValidLine = TestOrders.Line("SKU-1");

    // ---- The reported case: a default line snapshot ---------------------------------------------

    [Fact]
    public void Submit_SingleDefaultLine_IsRejected()
    {
        // The defect: one default line "agreed" with itself on currency (null == null) and the
        // derived total came out as default(Money), so submission succeeded with an order that
        // could never be confirmed.
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, [default]);
        });

        Assert.Equal("lines", exception.ParamName);
        Assert.Contains("not a valid order line snapshot", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rehydrate_SingleDefaultLine_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Rehydrate(TestOrders.OrderId, TestOrders.CustomerId, [default], OrderStatus.PendingPayment, currentAttempt: 2);
        });

        Assert.Equal("lines", exception.ParamName);
        Assert.Contains("not a valid order line snapshot", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Submit_DefaultLineAmongValidLines_IsRejected_AsAnInvalidLine_NotAsACurrencyMismatch(bool defaultFirst)
    {
        // A default line inside a valid order used to be reported as "mixed currency" only because
        // the other line happened to be valid; the diagnosis has to name the line as invalid, and it
        // has to be rejected whichever position it occupies.
        IReadOnlyList<OrderLineSnapshot> lines = defaultFirst
            ? [default, ValidLine, TestOrders.Line("SKU-2")]
            : [TestOrders.Line("SKU-1"), TestOrders.Line("SKU-2"), default];

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, lines);
        });

        Assert.Equal("lines", exception.ParamName);
        Assert.Contains("not a valid order line snapshot", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("single currency", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Rehydrate_MultipleDefaultLines_AreRejectedConsistently(bool allDefault)
    {
        IReadOnlyList<OrderLineSnapshot> lines = allDefault
            ? [default, default]
            : [default, ValidLine, default];

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Rehydrate(TestOrders.OrderId, TestOrders.CustomerId, lines, OrderStatus.PendingInventory, Order.InitialAttempt);
        });

        Assert.Contains("not a valid order line snapshot", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Submit_InvalidLine_FailsFastBeforeTheTotalIsComputed()
    {
        // The other two lines alone would overflow the sum. Line validation must run first, so the
        // caller gets a validation error rather than an arithmetic one.
        var lines = new[]
        {
            TestOrders.Line("SKU-1", unitAmountMinor: 1L << 62),
            TestOrders.Line("SKU-2", unitAmountMinor: 1L << 62),
            default(OrderLineSnapshot),
        };

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, lines);
        });

        Assert.Equal("lines", exception.ParamName);
        Assert.Contains("Line 2", exception.Message, StringComparison.Ordinal);
    }

    // ---- Default identifiers --------------------------------------------------------------------

    [Fact]
    public void Submit_DefaultOrderId_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Submit(default, TestOrders.CustomerId, TestOrders.OneLine());
        });

        Assert.Equal("orderId", exception.ParamName);
        Assert.Contains("non-empty identifier", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Submit_DefaultCustomerId_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Submit(TestOrders.OrderId, default, TestOrders.OneLine());
        });

        Assert.Equal("customerId", exception.ParamName);
        Assert.Contains("non-empty identifier", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Submit_BothDefaultIdentifiers_AreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Submit(default, default, TestOrders.OneLine());
        });
    }

    [Fact]
    public void Rehydrate_DefaultOrderId_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Rehydrate(default, TestOrders.CustomerId, TestOrders.OneLine(), OrderStatus.Confirmed, currentAttempt: 3);
        });

        Assert.Equal("orderId", exception.ParamName);
    }

    [Fact]
    public void Rehydrate_DefaultCustomerId_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Rehydrate(TestOrders.OrderId, default, TestOrders.OneLine(), OrderStatus.Confirmed, currentAttempt: 3);
        });

        Assert.Equal("customerId", exception.ParamName);
    }

    [Fact]
    public void Rehydrate_EmptyGuidIdentifiers_AreRejectedLikeDefaultOnes()
    {
        // default(OrderId) and new OrderId(Guid.Empty) must not be treated differently, even though
        // the second one did run a constructor that would have rejected it.
        Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Rehydrate(
                default,
                new CustomerId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
                TestOrders.OneLine(),
                OrderStatus.PendingInventory,
                Order.InitialAttempt);
        });
    }

    [Fact]
    public void Submit_DefaultIdentifiersAndDefaultLine_AreRejected()
    {
        // The fully uninitialized call: every part of it is invalid, and nothing may be accepted.
        Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Submit(default, default, [default]);
        });
    }

    // ---- What stays allowed ---------------------------------------------------------------------

    [Fact]
    public void Submit_ValidLines_AndRealIdentifiers_StillProduceAValidOrder()
    {
        // The guards must not tighten into a refusal to accept anything: an ordinary order is
        // unchanged, its total is valid money, and it can still reach Confirmed.
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.TwoLines());

        Assert.Equal(OrderStatus.PendingInventory, order.Status);
        Assert.Equal(TestOrders.OrderId, order.Id);
        Assert.Equal(TestOrders.CustomerId, order.CustomerId);
        Assert.True(order.Total.IsValid);
        Assert.Equal(new Money(2_250L, "USD"), order.Total);

        Assert.True(order.ApplyInventoryReserved(Order.InitialAttempt).IsApplied);
        Assert.True(order.ApplyPaymentAuthorized(Order.InitialAttempt, order.Total).IsApplied);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Submit_ZeroTotalOrder_IsStillAccepted_WithoutARejectedDefaultLine()
    {
        // A zero amount is valid money (PLAN.md §11: amount >= 0). Rejecting default values must not
        // be confused with rejecting free lines.
        var order = Order.Submit(
            TestOrders.OrderId,
            TestOrders.CustomerId,
            [TestOrders.Line("SKU-1", quantity: 3, unitAmountMinor: 0L)]);

        Assert.Equal(new Money(0L, "USD"), order.Total);
        Assert.True(order.Total.IsValid);
    }
}
