namespace Commerce.Domain.Orders;

/// <summary>
/// Money as an integer number of minor units plus a three-letter ISO 4217 alphabetic currency
/// code (PLAN.md §8, §24). Binary floating point is never used, and no external currency
/// dependency is introduced: the domain guarantees the code's shape and that one order uses one
/// currency. Minor-unit scale (100 cents per unit, and so on) is a property of the code itself and
/// is resolved by the sellable-product/pricing data owned by Commerce, not by this type.
/// </summary>
/// <remarks>
/// Amounts are nonnegative: the order domain models prices and totals, never debits or negatives.
/// Arithmetic uses <c>checked</c> semantics so an overflow fails fast instead of silently wrapping.
/// Instances must be created through the constructor so the invariants hold; <c>default(Money)</c>
/// is not a valid money value — it has no currency at all. Because this is a struct, that default
/// exists without running the constructor, so consumers that receive a money they did not create
/// recheck it through <see cref="IsValid"/>.
/// </remarks>
public readonly record struct Money
{
    private const int CurrencyCodeLength = 3;

    /// <summary>
    /// Creates and validates a money amount.
    /// </summary>
    /// <param name="amountMinor">Nonnegative amount in integer minor units.</param>
    /// <param name="currency">ISO 4217 alphabetic currency code; normalized to uppercase.</param>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="amountMinor"/> is negative.</exception>
    /// <exception cref="ArgumentException"><paramref name="currency"/> is not a three-letter alphabetic code.</exception>
    public Money(long amountMinor, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amountMinor);
        AmountMinor = amountMinor;
        Currency = NormalizeCurrency(currency);
    }

    /// <summary>Amount in integer minor units. Never negative.</summary>
    public long AmountMinor { get; }

    /// <summary>Normalized uppercase three-letter currency code.</summary>
    public string Currency { get; }

    /// <summary>
    /// True when this is a money the constructor could have produced: a nonnegative amount and a
    /// normalized three-letter uppercase alphabetic currency. False for <c>default(Money)</c>, whose
    /// currency is <see langword="null"/>, because that value bypasses the constructor.
    /// </summary>
    public bool IsValid => AmountMinor >= 0 && IsNormalizedCurrencyCode(Currency);

    /// <summary>
    /// Adds two amounts of the same currency.
    /// </summary>
    /// <exception cref="ArgumentException">The currencies differ (one currency per order).</exception>
    /// <exception cref="OverflowException">The sum exceeds the range of <see cref="long"/>.</exception>
    public Money Add(Money other)
    {
        ValidateSameCurrency(other);
        return new Money(checked(AmountMinor + other.AmountMinor), Currency);
    }

    /// <summary>
    /// Multiplies the amount by a whole factor using checked arithmetic, so an unsafe product fails
    /// fast instead of wrapping. This is how a line total (quantity × unit amount) is derived.
    /// </summary>
    /// <exception cref="OverflowException">The product exceeds the range of <see cref="long"/>.</exception>
    public Money Multiply(int factor) => new(checked(AmountMinor * factor), Currency);

    /// <summary>True when both amounts share the same normalized currency code.</summary>
    public bool IsSameCurrencyAs(Money other)
        => string.Equals(Currency, other.Currency, StringComparison.Ordinal);

    /// <summary>
    /// The domain text form used by diagnostics: "<see cref="Currency"/>
    /// <see cref="AmountMinor"/>" in minor units, never formatted as a decimal currency value.
    /// </summary>
    public override string ToString() => $"{Currency} {AmountMinor}";

    /// <exception cref="ArgumentException">The currencies differ.</exception>
    private void ValidateSameCurrency(Money other)
    {
        if (!IsSameCurrencyAs(other))
        {
            throw new ArgumentException(
                $"Money currency mismatch: {Currency} and {other.Currency} cannot be combined; an order uses a single currency.",
                nameof(other));
        }
    }

    private static string NormalizeCurrency(string currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        var normalized = currency.Trim().ToUpperInvariant();
        if (!IsNormalizedCurrencyCode(normalized))
        {
            throw new ArgumentException(
                $"Currency '{currency}' must be a three-letter ISO 4217 alphabetic code.",
                nameof(currency));
        }

        return normalized;
    }

    /// <summary>
    /// The shape rule a stored currency code must satisfy: exactly three <c>A–Z</c> letters, already
    /// uppercased. Shared by the constructor (which normalizes first) and <see cref="IsValid"/>, so
    /// the two can never disagree about what a valid money is.
    /// </summary>
    private static bool IsNormalizedCurrencyCode(string? code)
    {
        if (code is not { Length: CurrencyCodeLength })
        {
            return false;
        }

        foreach (var character in code)
        {
            if (character is < 'A' or > 'Z')
            {
                return false;
            }
        }

        return true;
    }
}
