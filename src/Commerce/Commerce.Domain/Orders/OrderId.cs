namespace Commerce.Domain.Orders;

/// <summary>
/// Opaque, stable identifier of an <see cref="Order"/> (PLAN.md §8, §24). A value object so order
/// identity cannot be confused with customer identity or any other identifier at call sites.
/// </summary>
/// <remarks>
/// <see cref="Guid.Empty"/> is rejected: it is not a usable identity, and rejecting it prevents an
/// uninitialized identifier from silently becoming an order key. Because this is a struct,
/// <c>default(OrderId)</c> exists without running that constructor, so consumers that receive an
/// identifier they did not create recheck it through <see cref="IsValid"/>.
/// </remarks>
public readonly record struct OrderId
{
    /// <summary>
    /// Creates an order identifier.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public OrderId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An order id must be a non-empty identifier.", nameof(value));
        }

        Value = value;
    }

    /// <summary>The underlying opaque identifier.</summary>
    public Guid Value { get; }

    /// <summary>
    /// True when this is a usable identifier, i.e. the one the constructor produces. False for
    /// <c>default(OrderId)</c>, which bypasses the constructor.
    /// </summary>
    public bool IsValid => Value != Guid.Empty;

    /// <summary>Text form of the identifier, for correlation and safe diagnostics only.</summary>
    public override string ToString() => Value.ToString();
}
