using ThemePark.Refunds.Models;
using ThemePark.Shared.Records;

namespace ThemePark.Tests.Shared.Assertions;

/// <summary>Represents a single refund record for assertion purposes.</summary>
public sealed record RefundRecord(string PassengerId, decimal Amount, bool ReceivedVoucher);

/// <summary>xUnit assertion helpers for refund-related test assertions.</summary>
public static class RefundAssertions
{
    /// <summary>
    /// Asserts that every passenger in <paramref name="passengers"/> appears
    /// in <paramref name="refundBatch"/>'s Passengers list.
    /// </summary>
    public static void AssertAllPassengersRefunded(
        IEnumerable<Passenger> passengers,
        RefundBatch refundBatch)
    {
        ArgumentNullException.ThrowIfNull(passengers);
        ArgumentNullException.ThrowIfNull(refundBatch);

        var refundedIds = refundBatch.Passengers
            .Select(p => p.PassengerId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var passenger in passengers)
        {
            Xunit.Assert.Contains(passenger.PassengerId, refundedIds);
        }
    }

    /// <summary>
    /// Converts a <see cref="RefundBatch"/> to an <see cref="IEnumerable{RefundRecord}"/>.
    /// Amount is €10.00 per passenger; VIP passengers also receive a voucher.
    /// </summary>
    public static IEnumerable<RefundRecord> ToRefundRecords(RefundBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        return batch.Passengers.Select(p =>
            new RefundRecord(p.PassengerId, 10.00m, p.IsVip));
    }
}
