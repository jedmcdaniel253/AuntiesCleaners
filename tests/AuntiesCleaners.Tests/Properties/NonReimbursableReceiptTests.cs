using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Services;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// P8: Non-Reimbursable Receipt Billing — verify non-reimbursable receipts show $0.00
/// and contribute $0 to totals.
/// **Validates: Requirements 7.9, 7.11**
/// </summary>
public class NonReimbursableReceiptTests
{
    private static Gen<decimal> PositiveAmountGen =>
        Gen.Choose(1, 100000).Select(i => (decimal)i / 100m);

    private static Gen<Receipt> ReceiptGen(bool isReimbursable) =>
        from amount in PositiveAmountGen
        from nameIdx in Gen.Choose(1, 1000)
        from dayOffset in Gen.Choose(0, 365)
        select new Receipt
        {
            Id = Guid.NewGuid(),
            WorkerId = Guid.NewGuid(),
            ReceiptDate = DateTime.Today.AddDays(-dayOffset),
            BusinessName = $"Store-{nameIdx}",
            Amount = amount,
            IsReimbursable = isReimbursable,
            PhotoUrl = "https://example.com/photo.jpg",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Property 1: Non-reimbursable receipts show $0.00 in the billing report.
    /// **Validates: Requirements 7.9**
    /// </summary>
    [Property]
    public Property NonReimbursableReceiptsShowZeroAmount()
    {
        var gen = Gen.ListOf(ReceiptGen(false)).Where(l => l.Count > 0);

        return Prop.ForAll(
            Arb.From(gen),
            receipts =>
            {
                var section = BillingReportService.BuildReceiptSection(receipts.ToList());

                return section.LineItems.All(li => li.Amount == 0m);
            });
    }

    /// <summary>
    /// Property 2: Non-reimbursable receipts contribute $0 to the section subtotal.
    /// **Validates: Requirements 7.11**
    /// </summary>
    [Property]
    public Property NonReimbursableReceiptsContributeZeroToSubtotal()
    {
        var gen = Gen.ListOf(ReceiptGen(false)).Where(l => l.Count > 0);

        return Prop.ForAll(
            Arb.From(gen),
            receipts =>
            {
                var section = BillingReportService.BuildReceiptSection(receipts.ToList());

                return section.Subtotal == 0m;
            });
    }

    /// <summary>
    /// Property 3: Reimbursable receipts show their actual amount.
    /// **Validates: Requirements 7.9**
    /// </summary>
    [Property]
    public Property ReimbursableReceiptsShowActualAmount()
    {
        var gen = Gen.ListOf(ReceiptGen(true)).Where(l => l.Count > 0);

        return Prop.ForAll(
            Arb.From(gen),
            receipts =>
            {
                var receiptList = receipts.ToList();
                var section = BillingReportService.BuildReceiptSection(receiptList);

                for (int i = 0; i < receiptList.Count; i++)
                {
                    if (section.LineItems[i].Amount != receiptList[i].Amount)
                        return false;
                }
                return true;
            });
    }

    /// <summary>
    /// Property 4: Mixed receipts — only reimbursable amounts contribute to subtotal.
    /// **Validates: Requirements 7.9, 7.11**
    /// </summary>
    [Property]
    public Property MixedReceiptsSubtotalOnlyIncludesReimbursable()
    {
        var mixedGen =
            from reimbursable in Gen.ListOf(ReceiptGen(true))
            from nonReimbursable in Gen.ListOf(ReceiptGen(false))
            let all = reimbursable.Concat(nonReimbursable).ToList()
            where all.Count > 0
            select (all, expectedTotal: reimbursable.Sum(r => r.Amount));

        return Prop.ForAll(
            Arb.From(mixedGen),
            tuple =>
            {
                var (receipts, expectedTotal) = tuple;
                var section = BillingReportService.BuildReceiptSection(receipts);

                return section.Subtotal == expectedTotal;
            });
    }
}
