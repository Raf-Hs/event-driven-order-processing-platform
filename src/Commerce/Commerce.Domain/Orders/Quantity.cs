namespace Commerce.Domain.Orders;

/// <summary>
/// A positive, whole number of units on an order line (PLAN.md §8: "Quantity is positive and
/// bounded"). Zero and negative quantities are rejected at construction, so a line can never
/// silently reduce an order total.
/// </summary>
/// <remarks>
/// The bound is the positive range of <see cref="int"/>, combined with the <c>checked</c>
/// arithmetic in <see cref="Money.Multiply(int)"/> that consumes it: an implausible quantity can
/// only produce a correct product or fail fast, never a wrapped total. Any smaller
/// request-level cap (per-line or per-order limits used for abuse protection) is transport
/// validation and belongs to the API phase (PLAN.md §12), not to the domain invariant.
/// <para>
/// Because this is a struct, <c>default(Quantity)</c> exists without running that constructor and
/// has value zero, so consumers that receive a quantity they did not create recheck it through
/// <see cref="IsValid"/>.
/// </para>
/// </remarks>
public readonly record struct Quantity
{
    /// <summary>
    /// Creates a quantity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is zero or negative.</exception>
    public Quantity(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        Value = value;
    }

    /// <summary>The whole number of units. Always greater than zero.</summary>
    public int Value { get; }

    /// <summary>
    /// True when this is a positive quantity, i.e. the one the constructor produces. False for
    /// <c>default(Quantity)</c>, which bypasses the constructor and is zero.
    /// </summary>
    public bool IsValid => Value > 0;
}
