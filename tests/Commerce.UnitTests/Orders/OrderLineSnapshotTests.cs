using Commerce.Domain.Orders;

namespace Commerce.UnitTests.Orders;

/// <summary>
/// Order lines are immutable snapshots of what was sold (PLAN.md §8): SKU, name, quantity and unit
/// amount, with a derived line total computed under checked arithmetic.
/// </summary>
public class OrderLineSnapshotTests
{
    [Fact]
    public void LineTotal_IsQuantityTimesUnitAmount_InTheSameCurrency()
    {
        var line = new OrderLineSnapshot(
            new Sku("SKU-1"),
            "Widget",
            new Quantity(3),
            new Money(999L, "USD"));

        Assert.Equal(2_997L, line.LineTotal.AmountMinor);
        Assert.Equal("USD", line.LineTotal.Currency);
        Assert.Equal(new Sku("SKU-1"), line.Sku);
        Assert.Equal("Widget", line.DisplayName);
        Assert.Equal(new Quantity(3), line.Quantity);
        Assert.Equal(new Money(999L, "USD"), line.UnitAmount);
    }

    [Fact]
    public void LineTotal_CannotBeSuppliedByTheCaller()
    {
        // Zero-priced lines are allowed (PLAN.md §11: amount >= 0), so a free line totals zero.
        var line = new OrderLineSnapshot(new Sku("SKU-1"), "Free sample", new Quantity(2), new Money(0L, "USD"));

        Assert.Equal(0L, line.LineTotal.AmountMinor);
    }

    [Fact]
    public void LineTotal_BigQuantityTimesBigUnitAmount_IsExact()
    {
        // Both sides are within range and the checked product fits in long minor units.
        var line = new OrderLineSnapshot(new Sku("SKU-1"), "Bulk", new Quantity(1_000), new Money(10_000_000L, "USD"));

        Assert.Equal(10_000_000_000L, line.LineTotal.AmountMinor);
    }

    [Fact]
    public void LineTotal_Overflow_FailsFastInsteadOfWrapping()
    {
        Assert.Throws<OverflowException>(() =>
        {
            _ = new OrderLineSnapshot(new Sku("SKU-1"), "Widget", new Quantity(2), new Money(long.MaxValue, "USD"));
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DisplayName_Blank_IsRejected(string displayName)
    {
        // Only "a name exists" is required here; PLAN.md sets no name length or character rules.
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new OrderLineSnapshot(new Sku("SKU-1"), displayName, new Quantity(1), new Money(100L, "USD"));
        });
    }

    [Fact]
    public void DisplayName_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new OrderLineSnapshot(new Sku("SKU-1"), null!, new Quantity(1), new Money(100L, "USD"));
        });
    }

    [Fact]
    public void DisplayName_IsTrimmed()
    {
        var line = new OrderLineSnapshot(new Sku("SKU-1"), "  Widget  ", new Quantity(1), new Money(100L, "USD"));

        Assert.Equal("Widget", line.DisplayName);
    }

    [Fact]
    public void Snapshots_AreValueEqual_WhenEveryFieldMatches()
    {
        var first = TestOrders.Line("SKU-1", quantity: 2, unitAmountMinor: 1_000L);
        var second = new OrderLineSnapshot(new Sku("SKU-1"), "SKU-1 test product", new Quantity(2), new Money(1_000L, "USD"));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Snapshots_Differ_WhenAnySnapshotFieldDiffers()
    {
        var baseline = TestOrders.Line("SKU-1", quantity: 2, unitAmountMinor: 1_000L);

        Assert.NotEqual(baseline, TestOrders.Line("SKU-9", quantity: 2, unitAmountMinor: 1_000L));
        Assert.NotEqual(baseline, TestOrders.Line("SKU-1", quantity: 3, unitAmountMinor: 1_000L));
        Assert.NotEqual(baseline, TestOrders.Line("SKU-1", quantity: 2, unitAmountMinor: 1_001L));
        Assert.NotEqual(baseline, TestOrders.Line("SKU-1", quantity: 2, unitAmountMinor: 1_000L, currency: "EUR"));
    }

    [Fact]
    public void ToString_ContainsNoCustomerOrPaymentData()
    {
        var line = TestOrders.Line("SKU-1", quantity: 2, unitAmountMinor: 1_000L);

        Assert.Equal("SKU-1 x 2 @ USD 1000", line.ToString());
    }

    // ---- Constructor boundary: value-object arguments are rechecked (M1-01-FIX-1) ----------------

    [Fact]
    public void Ctor_DefaultSku_IsRejected()
    {
        // The snapshot used to copy whatever Sku struct it was given, including one that never ran
        // the Sku constructor and so holds no text.
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = new OrderLineSnapshot(default, "Widget", new Quantity(1), new Money(100L, "USD"));
        });

        Assert.Equal("sku", exception.ParamName);
    }

    [Fact]
    public void Ctor_DefaultQuantity_IsRejected_AsNonPositive()
    {
        // default(Quantity) is zero, and the line total silently became Money(0, currency).
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new OrderLineSnapshot(new Sku("SKU-1"), "Widget", default, new Money(100L, "USD"));
        });

        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void Ctor_DefaultUnitAmount_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = new OrderLineSnapshot(new Sku("SKU-1"), "Widget", new Quantity(2), default);
        });

        Assert.Equal("unitAmount", exception.ParamName);
    }

    [Fact]
    public void Ctor_AllArgumentsValid_Succeeds()
    {
        var line = new OrderLineSnapshot(new Sku("SKU-1"), "Widget", new Quantity(2), new Money(100L, "USD"));

        Assert.Equal(new Money(200L, "USD"), line.LineTotal);
    }

    // ---- IsValid: the aggregate's recheck of a line it did not create ----------------------------

    [Fact]
    public void IsValid_ConstructorValue_IsTrue()
    {
        Assert.True(TestOrders.Line("SKU-1", quantity: 2, unitAmountMinor: 1_000L).IsValid);
        Assert.True(new OrderLineSnapshot(new Sku("SKU-1"), "Free sample", new Quantity(3), new Money(0L, "USD")).IsValid);
    }

    [Fact]
    public void IsValid_DefaultLineSnapshot_IsFalse()
    {
        // The reported defect: a single default line agreed with itself on currency (null == null)
        // and an order was created around it.
        Assert.False(default(OrderLineSnapshot).IsValid);
    }
}
