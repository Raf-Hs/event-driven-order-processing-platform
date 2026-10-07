namespace Commerce.Domain.Orders;

/// <summary>
/// An immutable snapshot of one sold line (PLAN.md §8: "A line cannot change after submission in
/// MVP"). The SKU and display name are copied from the Commerce-owned sellable product at
/// submission, so later catalog edits cannot rewrite order history, and the unit amount is the
/// seller price the customer was shown. Line total is quantity × unit amount under
/// <c>checked</c> arithmetic, computed once at construction.
/// </summary>
/// <remarks>
/// Every argument is rechecked, not just trusted: the value objects this snapshot is built from are
/// structs, and a struct's <c>default</c> value exists without ever running its constructor, so
/// <c>default(Sku)</c>, <c>default(Quantity)</c> and <c>default(Money)</c> can be passed in. The
/// members are get-only, so nothing outside can alter a snapshot afterwards, but the values handed
/// <em>in</em> have to satisfy <see cref="IsValid"/> for the snapshot to be valid.
/// </remarks>
public readonly record struct OrderLineSnapshot
{
    /// <summary>
    /// Creates a line snapshot and its derived total.
    /// </summary>
    /// <param name="sku">Sold SKU.</param>
    /// <param name="displayName">Sellable product name copied at submission.</param>
    /// <param name="quantity">Positive number of units.</param>
    /// <param name="unitAmount">Nonnegative unit price in integer minor units plus currency.</param>
    /// <exception cref="ArgumentException"><paramref name="sku"/> is not a valid SKU, or
    /// <paramref name="displayName"/> is empty or whitespace only, or
    /// <paramref name="unitAmount"/> is not a valid money amount.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not a positive
    /// quantity (zero, which is <c>default(Quantity)</c>, or a negative value).</exception>
    /// <exception cref="OverflowException">Quantity × unit amount exceeds the range of <see cref="long"/> minor units.</exception>
    public OrderLineSnapshot(Sku sku, string displayName, Quantity quantity, Money unitAmount)
    {
        if (!sku.IsValid)
        {
            throw new ArgumentException(
                "A line must be sold against a non-empty SKU; a default SKU is not one.",
                nameof(sku));
        }

        ArgumentNullException.ThrowIfNull(displayName);

        var name = displayName.Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("A line display name must not be empty.", nameof(displayName));
        }

        if (!quantity.IsValid)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity.Value,
                "A line must carry a positive quantity; a default quantity is zero.");
        }

        if (!unitAmount.IsValid)
        {
            throw new ArgumentException(
                "A line must carry a unit amount with a nonnegative minor-unit value and a three-letter currency; a default money amount is not one.",
                nameof(unitAmount));
        }

        Sku = sku;
        DisplayName = name;
        Quantity = quantity;
        UnitAmount = unitAmount;
        LineTotal = unitAmount.Multiply(quantity.Value);
    }

    /// <summary>Sold SKU.</summary>
    public Sku Sku { get; }

    /// <summary>Product name captured at submission.</summary>
    public string DisplayName { get; }

    /// <summary>Positive quantity sold.</summary>
    public Quantity Quantity { get; }

    /// <summary>Unit price at submission (minor units + currency).</summary>
    public Money UnitAmount { get; }

    /// <summary>
    /// <see cref="Quantity"/> × <see cref="UnitAmount"/> in minor units, same currency. Derived,
    /// never supplied by a caller.
    /// </summary>
    public Money LineTotal { get; }

    /// <summary>
    /// True when every member of this snapshot is a value its constructor would have produced: a
    /// non-empty SKU, a non-blank display name, a positive quantity, and a valid unit amount. False
    /// for <c>default(OrderLineSnapshot)</c>, which bypasses the constructor entirely.
    /// </summary>
    public bool IsValid =>
        Sku.IsValid
        && !string.IsNullOrWhiteSpace(DisplayName)
        && Quantity.IsValid
        && UnitAmount.IsValid;

    /// <summary>Diagnostic text form; contains no customer or payment data.</summary>
    public override string ToString() => $"{Sku} x {Quantity.Value} @ {UnitAmount}";
}
