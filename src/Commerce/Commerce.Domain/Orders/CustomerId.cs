namespace Commerce.Domain.Orders;

/// <summary>
/// Identifier of the Commerce customer (authenticated principal) that submitted an
/// <see cref="Order"/> (PLAN.md §8). Ownership checks in the API phase read this value; identity
/// data itself is never copied into the order.
/// </summary>
/// <remarks>
/// <see cref="Guid.Empty"/> is rejected so an anonymous or unresolved principal cannot be recorded
/// as an order owner. Because this is a struct, <c>default(CustomerId)</c> exists without running
/// that constructor, so consumers that receive a customer identifier they did not create recheck it
/// through <see cref="IsValid"/>.
/// </remarks>
public readonly record struct CustomerId
{
    /// <summary>
    /// Creates a customer identifier.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public CustomerId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A customer id must be a non-empty identifier.", nameof(value));
        }

        Value = value;
    }

    /// <summary>The underlying opaque identifier.</summary>
    public Guid Value { get; }

    /// <summary>
    /// True when this identifies a principal, i.e. the value the constructor produces. False for
    /// <c>default(CustomerId)</c>, which bypasses the constructor.
    /// </summary>
    public bool IsValid => Value != Guid.Empty;

    /// <summary>Text form of the identifier, for correlation and safe diagnostics only.</summary>
    public override string ToString() => Value.ToString();
}
