using Commerce.Domain.Orders;

namespace Commerce.UnitTests.Orders;

/// <summary>
/// Money is integer minor units plus a three-letter ISO currency code (PLAN.md §8, §24): nonnegative,
/// single-currency arithmetic, and checked so it can never wrap silently.
/// </summary>
public class MoneyTests
{
    [Fact]
    public void Constructor_PositiveAmountMinor_StoresValueAndCurrency()
    {
        var money = new Money(1_050L, "USD");

        Assert.Equal(1_050L, money.AmountMinor);
        Assert.Equal("USD", money.Currency);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    public void Constructor_NonnegativeAmountMinor_IsAccepted(long amountMinor)
    {
        // PLAN.md §11 allows unit amount >= 0: zero is a valid amount, not an error.
        Assert.Equal(amountMinor, new Money(amountMinor, "USD").AmountMinor);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void Constructor_NegativeAmountMinor_ThrowsArgumentOutOfRange(long amountMinor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new Money(amountMinor, "USD"); });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("US1")]
    [InlineData("1US")]
    [InlineData("U$D")]
    [InlineData("us d")]
    public void Constructor_InvalidCurrencyCode_ThrowsArgument(string currency)
    {
        Assert.Throws<ArgumentException>(() => { _ = new Money(100L, currency); });
    }

    [Fact]
    public void Constructor_NullCurrency_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = new Money(100L, null!); });
    }

    [Theory]
    [InlineData("usd", "USD")]
    [InlineData(" eur ", "EUR")]
    [InlineData("JPY", "JPY")]
    public void Constructor_CurrencyCode_IsNormalizedToTrimmedUppercase(string input, string expected)
    {
        Assert.Equal(expected, new Money(100L, input).Currency);
    }

    [Fact]
    public void Add_SameCurrency_SumsMinorUnits()
    {
        var sum = new Money(1_000L, "USD").Add(new Money(250L, "usd"));

        Assert.Equal(1_250L, sum.AmountMinor);
        Assert.Equal("USD", sum.Currency);
    }

    [Fact]
    public void Add_DifferentCurrency_ThrowsArgument_BecauseOneOrderUsesOneCurrency()
    {
        var usd = new Money(1_000L, "USD");
        var eur = new Money(1_000L, "EUR");

        Assert.Throws<ArgumentException>(() => { _ = usd.Add(eur); });
    }

    [Fact]
    public void Add_Overflow_ThrowsOverflowInsteadOfWrapping()
    {
        var max = new Money(long.MaxValue, "USD");

        Assert.Throws<OverflowException>(() => { _ = max.Add(new Money(1L, "USD")); });
    }

    [Theory]
    [InlineData(1_000L, 3, 3_000L)]
    [InlineData(1_000L, 1, 1_000L)]
    [InlineData(999L, 7, 6_993L)]
    public void Multiply_WholeFactor_ComputesExactProduct(long amountMinor, int factor, long expectedMinor)
    {
        var product = new Money(amountMinor, "USD").Multiply(factor);

        Assert.Equal(expectedMinor, product.AmountMinor);
        Assert.Equal("USD", product.Currency);
    }

    [Fact]
    public void Multiply_ByZero_ProducesZeroAmountInSameCurrency()
    {
        var product = new Money(1_000L, "USD").Multiply(0);

        Assert.Equal(0L, product.AmountMinor);
        Assert.Equal("USD", product.Currency);
    }

    [Fact]
    public void Multiply_MaximumAmountByTwo_ThrowsOverflowInsteadOfWrapping()
    {
        Assert.Throws<OverflowException>(() => { _ = new Money(long.MaxValue, "USD").Multiply(2); });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Multiply_NegativeFactor_CannotProduceNegativeMoney(int factor)
    {
        // Money's own nonnegative invariant is re-checked by the constructor, so a negative factor
        // fails fast instead of creating a debit-shaped value.
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new Money(100L, "USD").Multiply(factor); });
    }

    [Fact]
    public void Equality_IsValueBased_OnAmountAndCurrency()
    {
        Assert.Equal(new Money(1_000L, "USD"), new Money(1_000L, "USD"));
        Assert.True(new Money(1_000L, "usd") == new Money(1_000L, "USD"));
    }

    [Fact]
    public void Equality_DetectsAmountOrCurrencyDifference()
    {
        Assert.NotEqual(new Money(1_000L, "USD"), new Money(1_001L, "USD"));
        Assert.NotEqual(new Money(1_000L, "USD"), new Money(1_000L, "EUR"));
        Assert.True(new Money(1_000L, "USD") != new Money(1_000L, "EUR"));
    }

    [Fact]
    public void IsSameCurrencyAs_ComparesNormalizedCodes()
    {
        Assert.True(new Money(1L, "USD").IsSameCurrencyAs(new Money(999L, "usd")));
        Assert.False(new Money(1L, "USD").IsSameCurrencyAs(new Money(1L, "EUR")));
    }

    [Fact]
    public void ToString_ReportsCurrencyAndMinorUnits_WithoutDecimalFormatting()
    {
        // Minor units are the storage truth; formatting for display is a presentation concern.
        Assert.Equal("USD 1050", new Money(1_050L, "usd").ToString());
    }

    // ---- IsValid: the recheck for values that never ran a constructor (M1-01-FIX-1) --------------

    [Theory]
    [InlineData(0L)]
    [InlineData(1_050L)]
    [InlineData(long.MaxValue)]
    public void IsValid_ConstructorValue_IsTrue(long amountMinor)
    {
        Assert.True(new Money(amountMinor, "USD").IsValid);
    }

    [Fact]
    public void IsValid_DefaultMoney_IsFalse_BecauseItsCurrencyIsNull()
    {
        // default(Money) passes the amount rule (zero) but has no currency at all. This is the value
        // that used to let a default order line produce an order total that nothing could authorize.
        Assert.False(default(Money).IsValid);
    }

    [Fact]
    public void IsValid_ZeroAmountIsStillValidMoney_SoFreeLinesAreNotRejected()
    {
        // Rejecting default values must not be mistaken for rejecting zero: PLAN.md §11 allows >= 0.
        Assert.True(new Money(0L, "USD").IsValid);
    }
}
