namespace Commerce.Domain.Orders;

/// <summary>
/// Stock-keeping-unit identifier snapshotted onto an order line (PLAN.md §8). Commerce stores the
/// SKU text it sold; Inventory owns the stock record keyed by the same SKU. Comparison is
/// ordinal — the domain does not invent case-folding or length rules that PLAN.md does not state.
/// </summary>
/// <remarks>
/// Whether a SKU exists, is active, and is purchasable is a catalog/Inventory question checked at
/// submission time by the application layer and the reservation workflow, not by this type.
/// <para>
/// Because this is a struct, <c>default(Sku)</c> exists without running that constructor and carries
/// no text at all, so consumers that receive a SKU they did not create recheck it through
/// <see cref="IsValid"/>.
/// </para>
/// </remarks>
public readonly record struct Sku
{
    /// <summary>
    /// Creates a SKU identifier. Surrounding whitespace is trimmed; blank values are rejected.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or whitespace only.</exception>
    public Sku(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A SKU must not be empty.", nameof(value));
        }

        Value = trimmed;
    }

    /// <summary>The SKU text as sold, trimmed of surrounding whitespace.</summary>
    public string Value { get; }

    /// <summary>
    /// True when this carries non-blank text, i.e. the value the constructor produces. False for
    /// <c>default(Sku)</c>, which bypasses the constructor and has no text.
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Value);

    /// <summary>Text form of the SKU.</summary>
    public override string ToString() => Value;
}
