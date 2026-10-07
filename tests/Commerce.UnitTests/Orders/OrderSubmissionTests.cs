using Commerce.Domain.Orders;

namespace Commerce.UnitTests.Orders;

/// <summary>
/// Submission invariants (PLAN.md §8): the order starts in <see cref="OrderStatus.PendingInventory"/>
/// at attempt 1, its total is derived from immutable line snapshots with checked arithmetic, and it
/// carries exactly one currency. Clients cannot supply an authoritative total.
/// </summary>
public class OrderSubmissionTests
{
    [Fact]
    public void Submit_InitialStatus_IsPendingInventory()
    {
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.OneLine());

        Assert.Equal(OrderStatus.PendingInventory, order.Status);
        Assert.False(order.IsTerminal);
    }

    [Fact]
    public void Submit_InitialAttempt_IsOne()
    {
        Assert.Equal(Order.InitialAttempt, TestOrders.Submitted().CurrentAttempt);
    }

    [Fact]
    public void Submit_KeepsIdentifiers()
    {
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.OneLine());

        Assert.Equal(TestOrders.OrderId, order.Id);
        Assert.Equal(TestOrders.CustomerId, order.CustomerId);
    }

    [Fact]
    public void Submit_TotalEqualsSumOfLineTotals()
    {
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.TwoLines());

        // 2 × 1000 + 1 × 250 = 2250 minor units.
        Assert.Equal(new Money(2_250L, "USD"), order.Total);
        Assert.Equal("USD", order.Total.Currency);
    }

    [Fact]
    public void Submit_SingleLine_TotalEqualsThatLineTotal()
    {
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, [TestOrders.Line("SKU-1", quantity: 4, unitAmountMinor: 500L)]);

        Assert.Equal(new Money(2_000L, "USD"), order.Total);
    }

    [Fact]
    public void Submit_AllZeroUnitAmounts_TotalIsZero()
    {
        // Zero unit amounts are valid per PLAN.md §11 (amount >= 0); the total must stay zero.
        var lines = new[]
        {
            TestOrders.Line("SKU-1", quantity: 3, unitAmountMinor: 0L),
            TestOrders.Line("SKU-2", quantity: 1, unitAmountMinor: 0L),
        };

        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, lines);

        Assert.Equal(0L, order.Total.AmountMinor);
    }

    [Fact]
    public void Submit_PreservesLineOrderAndCount()
    {
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.TwoLines());

        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(new Sku("SKU-1"), order.Lines[0].Sku);
        Assert.Equal(new Sku("SKU-2"), order.Lines[1].Sku);
    }

    [Fact]
    public void Submit_MixedCurrencies_IsRejected()
    {
        var lines = new[] { TestOrders.Line("SKU-1"), TestOrders.Line("SKU-2", currency: "EUR") };

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, lines);
        });

        Assert.Contains("single currency", exception.Message);
    }

    [Fact]
    public void Submit_NoLines_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, []);
        });

        Assert.Contains("at least one line", exception.Message);
    }

    [Fact]
    public void Submit_NullLines_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, null!);
        });
    }

    [Fact]
    public void Submit_TotalOverflow_FailsFastInsteadOfWrapping()
    {
        // Each line total fits in long minor units, but their sum cannot: 2^62 + 2^62 = 2^63.
        var lines = new[]
        {
            TestOrders.Line("SKU-1", unitAmountMinor: 1L << 62),
            TestOrders.Line("SKU-2", unitAmountMinor: 1L << 62),
        };

        Assert.Throws<OverflowException>(() =>
        {
            _ = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, lines);
        });
    }

    [Fact]
    public void Submit_CopiesTheCallersCollection_SoLaterMutationCannotChangeTheOrder()
    {
        var lines = new List<OrderLineSnapshot> { TestOrders.Line("SKU-1") };
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, lines);

        lines.Clear();
        lines.Add(TestOrders.Line("SKU-2", quantity: 99, unitAmountMinor: 99L));

        Assert.Single(order.Lines);
        Assert.Equal(new Sku("SKU-1"), order.Lines[0].Sku);
        Assert.Equal(new Money(1_000L, "USD"), order.Total);
    }

    [Fact]
    public void Submit_ExposedLinesCannotBeDowncastToTheMutableBackingArray()
    {
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.TwoLines());

        Assert.IsNotType<OrderLineSnapshot[]>(order.Lines);
    }

    [Fact]
    public void Submit_LinesCannotBeReplacedThroughTheExposedView()
    {
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.TwoLines());

        // The read-only view rejects writes, so an accepted order's lines stay immutable (§8).
        Assert.Throws<NotSupportedException>(() =>
        {
            ((IList<OrderLineSnapshot>)order.Lines)[0] = TestOrders.Line("SKU-9");
        });

        Assert.Equal(new Sku("SKU-1"), order.Lines[0].Sku);
    }

    [Fact]
    public void Rehydrate_RecomputesTotalFromLines()
    {
        var order = Order.Rehydrate(
            TestOrders.OrderId,
            TestOrders.CustomerId,
            TestOrders.TwoLines(),
            OrderStatus.PendingPayment,
            currentAttempt: 2);

        Assert.Equal(new Money(2_250L, "USD"), order.Total);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(2, order.CurrentAttempt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rehydrate_AttemptBelowInitial_IsRejected(int attempt)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = Order.Rehydrate(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.OneLine(), OrderStatus.PendingInventory, attempt);
        });
    }

    [Fact]
    public void Rehydrate_UndefinedStatus_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Rehydrate(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.OneLine(), (OrderStatus)99, Order.InitialAttempt);
        });
    }

    [Fact]
    public void Rehydrate_MixedCurrencies_IsRejected()
    {
        var lines = new[] { TestOrders.Line("SKU-1"), TestOrders.Line("SKU-2", currency: "EUR") };

        Assert.Throws<ArgumentException>(() =>
        {
            _ = Order.Rehydrate(TestOrders.OrderId, TestOrders.CustomerId, lines, OrderStatus.PendingInventory, Order.InitialAttempt);
        });
    }

    [Theory]
    [InlineData(OrderStatus.PendingInventory)]
    [InlineData(OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Failed)]
    public void Rehydrate_AcceptsEveryMvpStatus(OrderStatus status)
    {
        var order = Order.Rehydrate(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.OneLine(), status, Order.InitialAttempt);

        Assert.Equal(status, order.Status);
        Assert.Equal(status is OrderStatus.Confirmed or OrderStatus.Failed, order.IsTerminal);
    }
}
