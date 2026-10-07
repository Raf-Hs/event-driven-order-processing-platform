using Commerce.Domain.Orders;

namespace Commerce.UnitTests.Orders;

/// <summary>
/// Quantity must be positive (PLAN.md §8, §11 check "quantity > 0"), and the identifiers are opaque,
/// non-empty, value-compared. No SKU length/case rules are invented because PLAN.md states none.
/// </summary>
public class ValueObjectTests
{
    private static readonly Guid SampleGuid = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Quantity_PositiveValue_IsAccepted(int value)
    {
        // "Quantity is positive and bounded" (PLAN.md §8): the bound is the positive int range, and
        // every consumer uses checked arithmetic. Tighter request-level caps are API validation (§12).
        Assert.Equal(value, new Quantity(value).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Quantity_ZeroOrNegative_IsRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new Quantity(value); });
    }

    [Fact]
    public void Quantity_Equality_IsValueBased()
    {
        Assert.Equal(new Quantity(3), new Quantity(3));
        Assert.NotEqual(new Quantity(3), new Quantity(4));
    }

    [Theory]
    [InlineData("SKU-1")]
    [InlineData("a")]
    public void Sku_NonBlankValue_IsAccepted(string value)
    {
        Assert.Equal(value, new Sku(value).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Sku_EmptyOrWhitespace_IsRejected(string value)
    {
        Assert.Throws<ArgumentException>(() => { _ = new Sku(value); });
    }

    [Fact]
    public void Sku_NullValue_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = new Sku(null!); });
    }

    [Fact]
    public void Sku_TrimsSurroundingWhitespace()
    {
        Assert.Equal("SKU-1", new Sku("  SKU-1  ").Value);
    }

    [Fact]
    public void Sku_ComparisonIsOrdinal_CaseIsSignificant()
    {
        // The domain performs no case folding: "SKU-1" and "sku-1" are different SKUs unless the
        // owning service's catalog data says otherwise.
        Assert.NotEqual(new Sku("SKU-1"), new Sku("sku-1"));
        Assert.Equal(new Sku("SKU-1"), new Sku(" SKU-1 "));
    }

    [Fact]
    public void OrderId_EmptyGuid_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => { _ = new OrderId(Guid.Empty); });
    }

    [Fact]
    public void CustomerId_EmptyGuid_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => { _ = new CustomerId(Guid.Empty); });
    }

    [Fact]
    public void Identifiers_ExposeTheirGuid_AndCompareByValue()
    {
        Assert.Equal(SampleGuid, new OrderId(SampleGuid).Value);
        Assert.Equal(SampleGuid, new CustomerId(SampleGuid).Value);
        Assert.Equal(new OrderId(SampleGuid), new OrderId(SampleGuid));
        Assert.NotEqual(new OrderId(SampleGuid), new OrderId(Guid.Parse("44444444-4444-4444-4444-444444444444")));
    }

    [Fact]
    public void Identifiers_AreDistinctTypes_SoOrderAndCustomerIdsCannotBeConfused()
    {
        Assert.NotEqual<object>(new OrderId(SampleGuid), new CustomerId(SampleGuid));
    }

    [Fact]
    public void Identifiers_ToString_IsSafeForCorrelationAndLogs()
    {
        Assert.Equal(SampleGuid.ToString(), new OrderId(SampleGuid).ToString());
        Assert.Equal("SKU-1", new Sku("SKU-1").ToString());
    }

    // ---- IsValid: the recheck for values that never ran a constructor (M1-01-FIX-1) --------------

    [Fact]
    public void OrderId_ConstructorValue_IsValid()
    {
        Assert.True(new OrderId(SampleGuid).IsValid);
    }

    [Fact]
    public void OrderId_Default_IsNotValid()
    {
        // default(OrderId) skips the constructor that rejects Guid.Empty, so IsValid is what keeps
        // it out of the aggregate.
        Assert.False(default(OrderId).IsValid);
    }

    [Fact]
    public void CustomerId_ConstructorValue_IsValid()
    {
        Assert.True(new CustomerId(SampleGuid).IsValid);
    }

    [Fact]
    public void CustomerId_Default_IsNotValid()
    {
        Assert.False(default(CustomerId).IsValid);
    }

    [Fact]
    public void Quantity_PositiveValue_IsValid()
    {
        Assert.True(new Quantity(1).IsValid);
        Assert.True(new Quantity(int.MaxValue).IsValid);
    }

    [Fact]
    public void Quantity_Default_IsNotValid()
    {
        // default(Quantity) is zero, which the constructor forbids.
        Assert.False(default(Quantity).IsValid);
    }

    [Theory]
    [InlineData("SKU-1")]
    [InlineData("  SKU-1  ")]
    public void Sku_ConstructorValue_IsValid(string value)
    {
        Assert.True(new Sku(value).IsValid);
    }

    [Fact]
    public void Sku_Default_IsNotValid()
    {
        // default(Sku) carries no text at all: its string field is null.
        Assert.False(default(Sku).IsValid);
    }
}
