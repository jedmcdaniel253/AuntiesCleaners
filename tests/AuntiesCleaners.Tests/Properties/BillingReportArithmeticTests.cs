using AuntiesCleaners.Client.Models;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// P1: Billing Report Arithmetic — verify grand total = sum of section subtotals,
/// each subtotal = sum of line items.
/// **Validates: Requirements 7.10, 7.11**
/// </summary>
public class BillingReportArithmeticTests
{
    private static Gen<decimal> AmountGen =>
        Gen.Choose(-10000, 100000).Select(i => (decimal)i / 100m);

    private static Gen<BillingLineItem> LineItemGen =>
        from amount in AmountGen
        from descIdx in Gen.Choose(1, 1000)
        select new BillingLineItem
        {
            Description = $"Item-{descIdx}",
            Amount = amount
        };

    private static Gen<BillingSection> SectionGen =>
        from name in Gen.Elements("Cleaning", "Laundry", "Mowing", "Maintenance", "Miscellaneous", "Receipts")
        from items in Gen.ListOf(LineItemGen).Where(l => l.Count > 0)
        select new BillingSection
        {
            Name = name,
            LineItems = items.ToList()
        };

    private static Gen<BillingReport> ReportGen =>
        from sections in Gen.ListOf(SectionGen).Where(l => l.Count > 0)
        select new BillingReport
        {
            DateFrom = DateTime.Today.AddDays(-30),
            DateTo = DateTime.Today,
            Sections = sections.ToList()
        };

    /// <summary>
    /// Property 1: Grand total equals the sum of all section subtotals.
    /// **Validates: Requirements 7.11**
    /// </summary>
    [Property]
    public Property GrandTotalEqualsSumOfSectionSubtotals()
    {
        return Prop.ForAll(
            Arb.From(ReportGen),
            report =>
            {
                var expectedGrandTotal = report.Sections.Sum(s => s.Subtotal);
                return report.GrandTotal == expectedGrandTotal;
            });
    }

    /// <summary>
    /// Property 2: Each section subtotal equals the sum of its line item amounts.
    /// **Validates: Requirements 7.10**
    /// </summary>
    [Property]
    public Property SectionSubtotalEqualsSumOfLineItemAmounts()
    {
        return Prop.ForAll(
            Arb.From(SectionGen),
            section =>
            {
                var expectedSubtotal = section.LineItems.Sum(li => li.Amount);
                return section.Subtotal == expectedSubtotal;
            });
    }

    /// <summary>
    /// Property 3: A report with no sections has a grand total of zero.
    /// **Validates: Requirements 7.11**
    /// </summary>
    [Property]
    public Property EmptyReportHasZeroGrandTotal()
    {
        var report = new BillingReport
        {
            DateFrom = DateTime.Today.AddDays(-30),
            DateTo = DateTime.Today,
            Sections = new List<BillingSection>()
        };

        return (report.GrandTotal == 0m).ToProperty();
    }

    /// <summary>
    /// Property 4: A section with no line items has a subtotal of zero.
    /// **Validates: Requirements 7.10**
    /// </summary>
    [Property]
    public Property EmptySectionHasZeroSubtotal()
    {
        var section = new BillingSection
        {
            Name = "Empty",
            LineItems = new List<BillingLineItem>()
        };

        return (section.Subtotal == 0m).ToProperty();
    }
}
