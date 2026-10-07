using Commerce.Domain.Orders;

namespace Commerce.UnitTests.Orders;

/// <summary>
/// The MVP lifecycle from PLAN.md §9: PendingInventory → PendingPayment → Confirmed, with Failed
/// reachable from either pending state. Because delivery is at-least-once (§18), duplicate,
/// out-of-order, and superseded-attempt results must be recognized and must never mutate state.
/// </summary>
public class OrderTransitionTests
{
    private static readonly Money Usd1000 = new(1_000L, "USD");

    // ---- The four valid transitions -----------------------------------------------------------

    [Fact]
    public void ApplyInventoryReserved_FromPendingInventory_MovesToPendingPayment()
    {
        var order = TestOrders.Submitted();

        var result = order.ApplyInventoryReserved(Order.InitialAttempt);

        Assert.Equal(OrderTransitionOutcome.Applied, result.Outcome);
        Assert.True(result.IsApplied);
        Assert.Equal(OrderStatus.PendingInventory, result.StatusBefore);
        Assert.Equal(OrderStatus.PendingPayment, result.StatusAfter);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.False(order.IsTerminal);
    }

    [Fact]
    public void ApplyInventoryRejected_FromPendingInventory_MovesToFailed()
    {
        var order = TestOrders.Submitted();

        var result = order.ApplyInventoryRejected(Order.InitialAttempt);

        Assert.Equal(OrderTransitionOutcome.Applied, result.Outcome);
        Assert.Equal(OrderStatus.Failed, result.StatusAfter);
        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.True(order.IsTerminal);
    }

    [Fact]
    public void ApplyPaymentAuthorized_MatchingAmountAndCurrency_MovesToConfirmed()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment);

        var result = order.ApplyPaymentAuthorized(Order.InitialAttempt, order.Total);

        Assert.Equal(OrderTransitionOutcome.Applied, result.Outcome);
        Assert.Equal(OrderStatus.Confirmed, result.StatusAfter);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.True(order.IsTerminal);
    }

    [Fact]
    public void ApplyPaymentDeclined_FromPendingPayment_MovesToFailed()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment);

        var result = order.ApplyPaymentDeclined(Order.InitialAttempt);

        Assert.Equal(OrderTransitionOutcome.Applied, result.Outcome);
        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.True(order.IsTerminal);
    }

    [Fact]
    public void FullHappyPath_MovesThroughEveryMvpStateOnce()
    {
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.TwoLines());

        Assert.Equal(OrderStatus.PendingInventory, order.Status);
        Assert.True(order.ApplyInventoryReserved(Order.InitialAttempt).IsApplied);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);

        // The authorized amount must be the order's own derived total: 2 × 1000 + 250.
        Assert.True(order.ApplyPaymentAuthorized(Order.InitialAttempt, new Money(2_250L, "USD")).IsApplied);
        Assert.Equal(OrderStatus.Confirmed, order.Status);

        // Confirming never changes money or lines.
        Assert.Equal(new Money(2_250L, "USD"), order.Total);
        Assert.Equal(2, order.Lines.Count);
    }

    // ---- Duplicate results (at-least-once redelivery) -----------------------------------------

    [Fact]
    public void ApplyInventoryReserved_WhenAlreadyPendingPayment_IsAnIgnoredDuplicate()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment);

        var result = order.ApplyInventoryReserved(Order.InitialAttempt);

        Assert.Equal(OrderTransitionOutcome.IgnoredDuplicate, result.Outcome);
        Assert.False(result.IsApplied);
        Assert.Equal(OrderStatus.PendingPayment, result.StatusBefore);
        Assert.Equal(OrderStatus.PendingPayment, result.StatusAfter);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public void ApplyInventoryRejected_WhenAlreadyFailed_IsAnIgnoredDuplicate()
    {
        var order = TestOrders.InState(OrderStatus.Failed);

        var result = order.ApplyInventoryRejected(Order.InitialAttempt);

        Assert.Equal(OrderTransitionOutcome.IgnoredDuplicate, result.Outcome);
        Assert.Equal(OrderStatus.Failed, order.Status);
    }

    [Fact]
    public void ApplyPaymentDeclined_WhenAlreadyFailed_IsAnIgnoredDuplicate()
    {
        var order = TestOrders.InState(OrderStatus.Failed);

        Assert.Equal(OrderTransitionOutcome.IgnoredDuplicate, order.ApplyPaymentDeclined(Order.InitialAttempt).Outcome);
        Assert.Equal(OrderStatus.Failed, order.Status);
    }

    [Fact]
    public void ApplyPaymentAuthorized_WhenAlreadyConfirmed_IsAnIgnoredDuplicate()
    {
        var order = TestOrders.InState(OrderStatus.Confirmed);

        var result = order.ApplyPaymentAuthorized(Order.InitialAttempt, order.Total);

        Assert.Equal(OrderTransitionOutcome.IgnoredDuplicate, result.Outcome);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void RepeatedRedeliveryOfTheSameResult_NeverChangesStateAfterTheFirstApply()
    {
        var order = TestOrders.Submitted();

        Assert.True(order.ApplyInventoryReserved(Order.InitialAttempt).IsApplied);

        for (var delivery = 0; delivery < 5; delivery++)
        {
            var redelivery = order.ApplyInventoryReserved(Order.InitialAttempt);
            Assert.False(redelivery.IsApplied);
            Assert.Equal(OrderStatus.PendingPayment, redelivery.StatusAfter);
        }

        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    // ---- Out-of-order results: no advance, no regression --------------------------------------

    [Fact]
    public void ApplyPaymentAuthorized_WhilePendingInventory_IsIgnoredBecauseReservationMustComeFirst()
    {
        var order = TestOrders.Submitted();

        var result = order.ApplyPaymentAuthorized(Order.InitialAttempt, order.Total);

        // An order may not become confirmed until reservation AND authorization have succeeded (§8).
        Assert.Equal(OrderTransitionOutcome.IgnoredOutOfOrder, result.Outcome);
        Assert.Equal(OrderStatus.PendingInventory, order.Status);
    }

    [Fact]
    public void ApplyPaymentDeclined_WhilePendingInventory_IsIgnoredOutOfOrder()
    {
        var order = TestOrders.Submitted();

        Assert.Equal(OrderTransitionOutcome.IgnoredOutOfOrder, order.ApplyPaymentDeclined(Order.InitialAttempt).Outcome);
        Assert.Equal(OrderStatus.PendingInventory, order.Status);
    }

    [Fact]
    public void ApplyInventoryRejected_WhilePendingPayment_IsIgnoredAndDoesNotFailTheOrder()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment);

        var result = order.ApplyInventoryRejected(Order.InitialAttempt);

        Assert.Equal(OrderTransitionOutcome.IgnoredOutOfOrder, result.Outcome);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(OrderStatus.PendingPayment, result.StatusAfter);
    }

    [Fact]
    public void ApplyInventoryReserved_OnConfirmedOrder_DoesNotRegressState()
    {
        var order = TestOrders.InState(OrderStatus.Confirmed);

        var result = order.ApplyInventoryReserved(Order.InitialAttempt);

        Assert.Equal(OrderTransitionOutcome.IgnoredOutOfOrder, result.Outcome);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void ApplyPaymentDeclined_OnConfirmedOrder_DoesNotRegressState()
    {
        var order = TestOrders.InState(OrderStatus.Confirmed);

        Assert.Equal(OrderTransitionOutcome.IgnoredOutOfOrder, order.ApplyPaymentDeclined(Order.InitialAttempt).Outcome);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void ApplyInventoryReserved_OnFailedOrder_DoesNotRegressState()
    {
        var order = TestOrders.InState(OrderStatus.Failed);

        Assert.Equal(OrderTransitionOutcome.IgnoredOutOfOrder, order.ApplyInventoryReserved(Order.InitialAttempt).Outcome);
        Assert.Equal(OrderStatus.Failed, order.Status);
    }

    [Fact]
    public void ApplyPaymentAuthorized_OnFailedOrder_DoesNotConfirmAFailedOrder()
    {
        var order = TestOrders.InState(OrderStatus.Failed);

        Assert.Equal(OrderTransitionOutcome.IgnoredOutOfOrder, order.ApplyPaymentAuthorized(Order.InitialAttempt, order.Total).Outcome);
        Assert.Equal(OrderStatus.Failed, order.Status);
    }

    [Fact]
    public void TerminalConfirmedOrder_EveryResultIsANoOp()
    {
        var order = TestOrders.InState(OrderStatus.Confirmed);

        Assert.False(order.ApplyInventoryReserved(Order.InitialAttempt).IsApplied);
        Assert.False(order.ApplyInventoryRejected(Order.InitialAttempt).IsApplied);
        Assert.False(order.ApplyPaymentAuthorized(Order.InitialAttempt, order.Total).IsApplied);
        Assert.False(order.ApplyPaymentDeclined(Order.InitialAttempt).IsApplied);

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void TerminalFailedOrder_EveryResultIsANoOp()
    {
        var order = TestOrders.InState(OrderStatus.Failed);

        Assert.False(order.ApplyInventoryReserved(Order.InitialAttempt).IsApplied);
        Assert.False(order.ApplyInventoryRejected(Order.InitialAttempt).IsApplied);
        Assert.False(order.ApplyPaymentAuthorized(Order.InitialAttempt, order.Total).IsApplied);
        Assert.False(order.ApplyPaymentDeclined(Order.InitialAttempt).IsApplied);

        Assert.Equal(OrderStatus.Failed, order.Status);
    }

    // ---- Attempt guarding: superseded results must not touch the current order ----------------

    [Fact]
    public void ApplyInventoryRejected_FromSupersededAttempt_DoesNotFailTheCurrentAttempt()
    {
        // The saga moved this order on to attempt 2; attempt 1's rejection arrives late.
        var order = TestOrders.InState(OrderStatus.PendingInventory, attempt: 2);

        var result = order.ApplyInventoryRejected(attempt: 1);

        Assert.Equal(OrderTransitionOutcome.IgnoredStaleAttempt, result.Outcome);
        Assert.Equal(OrderStatus.PendingInventory, order.Status);
        Assert.Equal(2, order.CurrentAttempt);
    }

    [Fact]
    public void ApplyInventoryReserved_FromSupersededAttempt_DoesNotAdvanceTheCurrentAttempt()
    {
        var order = TestOrders.InState(OrderStatus.PendingInventory, attempt: 2);

        Assert.Equal(OrderTransitionOutcome.IgnoredStaleAttempt, order.ApplyInventoryReserved(attempt: 1).Outcome);
        Assert.Equal(OrderStatus.PendingInventory, order.Status);
    }

    [Fact]
    public void ApplyPaymentAuthorized_FromSupersededAttempt_IsIgnoredEvenWhenTheAmountMatches()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment, attempt: 3);

        var result = order.ApplyPaymentAuthorized(attempt: 2, order.Total);

        Assert.Equal(OrderTransitionOutcome.IgnoredStaleAttempt, result.Outcome);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public void ApplyPaymentDeclined_FromSupersededAttempt_IsIgnoredStaleAttempt()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment, attempt: 2);

        Assert.Equal(OrderTransitionOutcome.IgnoredStaleAttempt, order.ApplyPaymentDeclined(attempt: 1).Outcome);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public void ApplyInventoryReserved_ForAnAttemptTheOrderDoesNotKnowYet_IsIgnoredWithoutMutation()
    {
        var order = TestOrders.Submitted();

        var result = order.ApplyInventoryReserved(attempt: 2);

        Assert.Equal(OrderTransitionOutcome.IgnoredFutureAttempt, result.Outcome);
        Assert.Equal(OrderStatus.PendingInventory, order.Status);
        Assert.Equal(Order.InitialAttempt, order.CurrentAttempt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void MalformedAttemptNumbers_NeverMutateTheOrder(int attempt)
    {
        var order = TestOrders.Submitted();

        Assert.False(order.ApplyInventoryReserved(attempt).IsApplied);
        Assert.False(order.ApplyInventoryRejected(attempt).IsApplied);
        Assert.False(order.ApplyPaymentAuthorized(attempt, order.Total).IsApplied);
        Assert.False(order.ApplyPaymentDeclined(attempt).IsApplied);

        Assert.Equal(OrderStatus.PendingInventory, order.Status);
    }

    [Fact]
    public void AttemptGuardsAreCheckedBeforeStatusAndAmount_StaleMismatchReportsStale()
    {
        // A stale result with a wrong amount is still first and foremost a stale result: the attempt
        // guard runs before any state or amount evaluation, and nothing is mutated.
        var order = TestOrders.InState(OrderStatus.PendingPayment, attempt: 2);

        var result = order.ApplyPaymentAuthorized(attempt: 1, new Money(7L, "USD"));

        Assert.Equal(OrderTransitionOutcome.IgnoredStaleAttempt, result.Outcome);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public void CurrentAttemptResults_ApplyOnAttemptTwoJustAsTheyDoOnAttemptOne()
    {
        var order = TestOrders.InState(OrderStatus.PendingInventory, attempt: 2);

        Assert.True(order.ApplyInventoryReserved(attempt: 2).IsApplied);
        Assert.True(order.ApplyPaymentAuthorized(attempt: 2, order.Total).IsApplied);

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    // ---- Payment authorization must match amount AND currency ---------------------------------

    [Fact]
    public void ApplyPaymentAuthorized_AmountLowerThanTotal_IsRejectedWithoutConfirming()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment);

        var result = order.ApplyPaymentAuthorized(Order.InitialAttempt, new Money(999L, "USD"));

        Assert.Equal(OrderTransitionOutcome.RejectedPaymentMismatch, result.Outcome);
        Assert.False(result.IsApplied);
        Assert.Equal(OrderStatus.PendingPayment, result.StatusBefore);
        Assert.Equal(OrderStatus.PendingPayment, result.StatusAfter);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public void ApplyPaymentAuthorized_AmountHigherThanTotal_IsRejectedWithoutConfirming()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment);

        Assert.Equal(OrderTransitionOutcome.RejectedPaymentMismatch, order.ApplyPaymentAuthorized(Order.InitialAttempt, new Money(1_001L, "USD")).Outcome);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public void ApplyPaymentAuthorized_DifferentCurrencySameAmount_IsRejectedWithoutConfirming()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment);

        var result = order.ApplyPaymentAuthorized(Order.InitialAttempt, new Money(1_000L, "EUR"));

        Assert.Equal(OrderTransitionOutcome.RejectedPaymentMismatch, result.Outcome);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public void ApplyPaymentAuthorized_LowercaseCurrencyCode_StillMatchesNormalizedTotal()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment);

        var result = order.ApplyPaymentAuthorized(Order.InitialAttempt, new Money(1_000L, " usd "));

        Assert.Equal(OrderTransitionOutcome.Applied, result.Outcome);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void ApplyPaymentAuthorized_ZeroTotalOrder_ZeroAuthorizationMatches()
    {
        var order = Order.Rehydrate(
            TestOrders.OrderId,
            TestOrders.CustomerId,
            [TestOrders.Line("SKU-1", unitAmountMinor: 0L)],
            OrderStatus.PendingPayment,
            Order.InitialAttempt);

        Assert.Equal(0L, order.Total.AmountMinor);
        Assert.True(order.ApplyPaymentAuthorized(Order.InitialAttempt, Usd1000.Multiply(0)).IsApplied);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void ARejectedMismatchDoesNotLockTheOrder_ACorrectAuthorizationCanStillConfirmIt()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment);

        Assert.Equal(OrderTransitionOutcome.RejectedPaymentMismatch, order.ApplyPaymentAuthorized(Order.InitialAttempt, new Money(500L, "USD")).Outcome);
        Assert.Equal(OrderTransitionOutcome.Applied, order.ApplyPaymentAuthorized(Order.InitialAttempt, order.Total).Outcome);

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Transitions_NeverAlterTotalCurrencyOrLines()
    {
        var order = Order.Submit(TestOrders.OrderId, TestOrders.CustomerId, TestOrders.TwoLines());
        var totalBefore = order.Total;
        var lineCountBefore = order.Lines.Count;

        order.ApplyInventoryReserved(Order.InitialAttempt);
        order.ApplyPaymentAuthorized(Order.InitialAttempt, new Money(1L, "EUR"));
        order.ApplyPaymentAuthorized(Order.InitialAttempt, totalBefore);
        order.ApplyPaymentDeclined(Order.InitialAttempt);

        Assert.Equal(totalBefore, order.Total);
        Assert.Equal("USD", order.Total.Currency);
        Assert.Equal(lineCountBefore, order.Lines.Count);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void ResultStatusBeforeAndAfter_AreEqualForEveryNonAppliedOutcome()
    {
        var order = TestOrders.InState(OrderStatus.PendingPayment);

        var outcomes = new[]
        {
            order.ApplyInventoryReserved(Order.InitialAttempt),   // duplicate
            order.ApplyPaymentDeclined(attempt: 4),               // future attempt
            order.ApplyPaymentAuthorized(Order.InitialAttempt, Usd1000.Multiply(3)), // mismatch (3000 != 1000)
            order.ApplyInventoryRejected(Order.InitialAttempt),   // out of order
        };

        foreach (var result in outcomes)
        {
            Assert.False(result.IsApplied);
            Assert.Equal(result.StatusBefore, result.StatusAfter);
            Assert.Equal(OrderStatus.PendingPayment, result.StatusAfter);
        }
    }

    [Fact]
    public void StatusAndTotalExposeNoPublicSetter_OnlyWorkflowResultsAreAccepted()
    {
        // The aggregate must not offer an API that lets a caller write authoritative state (§6, §8):
        // a private setter exists for the aggregate's own use, but nothing outside can call it.
        var status = typeof(Order).GetProperty(nameof(Order.Status));
        var total = typeof(Order).GetProperty(nameof(Order.Total));

        Assert.NotNull(status);
        Assert.NotNull(total);
        Assert.True(status!.CanWrite);
        Assert.Null(status.GetSetMethod());
        Assert.Null(total!.GetSetMethod());
    }
}
