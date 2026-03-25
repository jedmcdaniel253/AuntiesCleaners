using AuntiesCleaners.Client.Models;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// P2: Worker Pay Arithmetic — verify each worker's total = sum of category pay amounts,
/// grand total = sum of worker totals.
/// **Validates: Requirements 8.7, 8.8**
/// </summary>
public class WorkerPayArithmeticTests
{
    private static Gen<decimal> PayAmountGen =>
        Gen.Choose(0, 100000).Select(i => (decimal)i / 100m);

    private static Gen<WorkerPayLineItem> LineItemGen =>
        from amount in PayAmountGen
        from descIdx in Gen.Choose(1, 1000)
        select new WorkerPayLineItem
        {
            Description = $"Item-{descIdx}",
            Amount = amount
        };

    private static Gen<List<WorkerPayLineItem>> LineItemListGen =>
        Gen.ListOf(LineItemGen).Select(l => l.ToList());

    private static Gen<WorkerPayment> WorkerPaymentGen =>
        from name in Gen.Elements("Alice", "Bob", "Charlie", "Diana", "Eve")
        from cleaningItems in LineItemListGen
        from laundryItems in LineItemListGen
        from maintenanceItems in LineItemListGen
        from lawnItems in LineItemListGen
        from miscItems in LineItemListGen
        select new WorkerPayment
        {
            WorkerName = name,
            CleaningItems = cleaningItems,
            CleaningPay = cleaningItems.Sum(i => i.Amount),
            LaundryItems = laundryItems,
            LaundryPay = laundryItems.Sum(i => i.Amount),
            MaintenanceItems = maintenanceItems,
            MaintenancePay = maintenanceItems.Sum(i => i.Amount),
            LawnItems = lawnItems,
            LawnPay = lawnItems.Sum(i => i.Amount),
            MiscItems = miscItems,
            MiscPay = miscItems.Sum(i => i.Amount)
        };

    private static Gen<WorkerPayReport> ReportGen =>
        from payments in Gen.ListOf(WorkerPaymentGen).Where(l => l.Count > 0)
        select new WorkerPayReport
        {
            DateFrom = DateTime.Today.AddDays(-30),
            DateTo = DateTime.Today,
            WorkerPayments = payments.ToList()
        };

    /// <summary>
    /// Property 1: Each worker's total equals the sum of their category pay amounts
    /// (CleaningPay + LaundryPay + MaintenancePay + LawnPay + MiscPay).
    /// **Validates: Requirements 8.7**
    /// </summary>
    [Property]
    public Property WorkerTotalEqualsSumOfCategoryPays()
    {
        return Prop.ForAll(
            Arb.From(WorkerPaymentGen),
            payment =>
            {
                var expectedTotal = payment.CleaningPay + payment.LaundryPay
                    + payment.MaintenancePay + payment.LawnPay + payment.MiscPay;
                return payment.Total == expectedTotal;
            });
    }

    /// <summary>
    /// Property 2: Grand total equals the sum of all worker totals.
    /// **Validates: Requirements 8.8**
    /// </summary>
    [Property]
    public Property GrandTotalEqualsSumOfWorkerTotals()
    {
        return Prop.ForAll(
            Arb.From(ReportGen),
            report =>
            {
                var expectedGrandTotal = report.WorkerPayments.Sum(wp => wp.Total);
                return report.GrandTotal == expectedGrandTotal;
            });
    }

    /// <summary>
    /// Property 3: A report with no worker payments has a grand total of zero.
    /// **Validates: Requirements 8.8**
    /// </summary>
    [Property]
    public Property EmptyReportHasZeroGrandTotal()
    {
        var report = new WorkerPayReport
        {
            DateFrom = DateTime.Today.AddDays(-30),
            DateTo = DateTime.Today,
            WorkerPayments = new List<WorkerPayment>()
        };

        return (report.GrandTotal == 0m).ToProperty();
    }

    /// <summary>
    /// Property 4: Each worker's category pay equals the sum of that category's line item amounts.
    /// **Validates: Requirements 8.7**
    /// </summary>
    [Property]
    public Property CategoryPayEqualsSumOfLineItems()
    {
        return Prop.ForAll(
            Arb.From(WorkerPaymentGen),
            payment =>
            {
                var cleaningMatch = payment.CleaningPay == payment.CleaningItems.Sum(i => i.Amount);
                var laundryMatch = payment.LaundryPay == payment.LaundryItems.Sum(i => i.Amount);
                var maintenanceMatch = payment.MaintenancePay == payment.MaintenanceItems.Sum(i => i.Amount);
                var lawnMatch = payment.LawnPay == payment.LawnItems.Sum(i => i.Amount);
                var miscMatch = payment.MiscPay == payment.MiscItems.Sum(i => i.Amount);
                return cleaningMatch && laundryMatch && maintenanceMatch && lawnMatch && miscMatch;
            });
    }
}
