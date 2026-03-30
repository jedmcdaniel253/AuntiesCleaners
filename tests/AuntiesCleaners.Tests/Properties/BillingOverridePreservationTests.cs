using System.Reflection;
using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Services;
using AuntiesCleaners.Tests.Helpers;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// Preservation — Property 2: Default Rate Fallback and Validation Unchanged.
/// These tests MUST PASS on unfixed code, confirming baseline behavior to preserve.
/// **Validates: Requirements 3.1, 3.2, 3.4, 3.5, 3.6**
/// </summary>
public class BillingOverridePreservationTests
{
    // ── Reflection helpers (same pattern as exploration tests) ──

    private static readonly MethodInfo BuildWorkSectionMethod =
        typeof(BillingReportService).GetMethod(
            "BuildWorkSection",
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo BuildMiscSectionMethod =
        typeof(BillingReportService).GetMethod(
            "BuildMiscSection",
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static BillingSection InvokeBuildWorkSection(
        string sectionName, WorkCategory category,
        List<WorkEntry> allEntries, List<Rate> rates,
        Dictionary<Guid, string> houseMap)
    {
        return (BillingSection)BuildWorkSectionMethod.Invoke(
            null, new object[] { sectionName, category, allEntries, rates, houseMap })!;
    }

    private static BillingSection InvokeBuildMiscSection(
        List<MiscellaneousEntry> entries,
        Dictionary<Guid, string> houseMap)
    {
        return (BillingSection)BuildMiscSectionMethod.Invoke(
            null, new object[] { entries, houseMap })!;
    }

    // ── Property 2a: Default Rate Fallback ──

    /// <summary>
    /// Property 2a: For any Cleaning work entry where NO worker-specific rate override exists
    /// (only a default rate with WorkerId == null), the billing amount equals
    /// hours × defaultRate.RateCharged.
    /// **Validates: Requirements 3.1, 3.2**
    /// </summary>
    [Property]
    public Property DefaultRateFallback_CleaningEntry_BillsAtDefaultRate()
    {
        return Prop.ForAll(
            Arb.From(Generators.CleaningEntryGen),
            Arb.From(Generators.PositiveDecimalGen),
            (entry, defaultRateCharged) =>
            {
                var defaultRate = new Rate
                {
                    Id = Guid.NewGuid(),
                    WorkCategoryValue = WorkCategory.Cleaning.ToString(),
                    WorkerId = null,
                    RateCharged = defaultRateCharged,
                    RatePaid = 10m,
                    UpdatedAt = DateTime.UtcNow
                };

                var ratesList = new List<Rate> { defaultRate };
                var houseMap = new Dictionary<Guid, string> { { entry.HouseId, "Test House" } };
                var allEntries = new List<WorkEntry> { entry };

                var section = InvokeBuildWorkSection("Cleaning", WorkCategory.Cleaning, allEntries, ratesList, houseMap);

                var expectedAmount = (entry.HoursBilled ?? 0) * defaultRateCharged;
                var actualAmount = section.LineItems.Sum(li => li.Amount);

                return actualAmount == expectedAmount;
            });
    }

    /// <summary>
    /// Property 2a (Maintenance): For any Maintenance work entry where NO worker-specific
    /// rate override exists, the billing amount equals hours × defaultRate.RateCharged.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Property]
    public Property DefaultRateFallback_MaintenanceEntry_BillsAtDefaultRate()
    {
        return Prop.ForAll(
            Arb.From(Generators.MaintenanceEntryGen),
            Arb.From(Generators.PositiveDecimalGen),
            (entry, defaultRateCharged) =>
            {
                var defaultRate = new Rate
                {
                    Id = Guid.NewGuid(),
                    WorkCategoryValue = WorkCategory.Maintenance.ToString(),
                    WorkerId = null,
                    RateCharged = defaultRateCharged,
                    RatePaid = 10m,
                    UpdatedAt = DateTime.UtcNow
                };

                var ratesList = new List<Rate> { defaultRate };
                var houseMap = new Dictionary<Guid, string> { { entry.HouseId, "Test House" } };
                var allEntries = new List<WorkEntry> { entry };

                var section = InvokeBuildWorkSection("Maintenance", WorkCategory.Maintenance, allEntries, ratesList, houseMap);

                var expectedAmount = (entry.HoursBilled ?? 0) * defaultRateCharged;
                var actualAmount = section.LineItems.Sum(li => li.Amount);

                return actualAmount == expectedAmount;
            });
    }

    // ── Property 2b: Default Rate Validation Rejects Zero ──

    /// <summary>
    /// Property 2b: For any default rate (WorkerId == null), RateService.ValidateRate(0m, anyPaid)
    /// throws ArgumentException for all non-negative paid values.
    /// **Validates: Requirements 3.4, 3.5**
    /// </summary>
    [Property]
    public Property DefaultRateValidation_RejectsZeroCharged()
    {
        return Prop.ForAll(
            Arb.From(Generators.NonNegativeDecimalGen),
            paid =>
            {
                try
                {
                    RateService.ValidateRate(0m, paid);
                    return false; // Should have thrown
                }
                catch (ArgumentException)
                {
                    return true; // Expected: $0.00 charged is rejected
                }
            });
    }

    // ── Property 2c: RatePaid Validation Unchanged ──

    /// <summary>
    /// Property 2c: For any rate, RateService.ValidateRate(positiveCharged, negativePaid)
    /// throws ArgumentException for all positive charged and negative paid values.
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Property]
    public Property RatePaidValidation_RejectsNegativePaid()
    {
        return Prop.ForAll(
            Arb.From(Generators.PositiveDecimalGen),
            Arb.From(Generators.PositiveDecimalGen),
            (charged, absPaid) =>
            {
                var negativePaid = -absPaid;
                try
                {
                    RateService.ValidateRate(charged, negativePaid);
                    return false; // Should have thrown
                }
                catch (ArgumentException)
                {
                    return true; // Expected: negative paid is rejected
                }
            });
    }

    // ── Property 2d: Misc and Receipt Billing Unchanged ──

    /// <summary>
    /// Property 2d (Misc): For any MiscellaneousEntry, the billing line item amount
    /// equals entry.ChargeAmount.
    /// **Validates: Requirements 3.6**
    /// </summary>
    [Property]
    public Property MiscBilling_AmountEqualsChargeAmount()
    {
        return Prop.ForAll(
            Arb.From(Generators.MiscellaneousEntryGen),
            entry =>
            {
                var houseMap = entry.HouseId.HasValue
                    ? new Dictionary<Guid, string> { { entry.HouseId.Value, "Test House" } }
                    : new Dictionary<Guid, string>();

                var entries = new List<MiscellaneousEntry> { entry };
                var section = InvokeBuildMiscSection(entries, houseMap);

                return section.LineItems.Count == 1
                    && section.LineItems[0].Amount == entry.ChargeAmount;
            });
    }

    /// <summary>
    /// Property 2d (Receipt): For any Receipt, the billing line item amount equals
    /// (IsReimbursable ? Amount : 0).
    /// **Validates: Requirements 3.6**
    /// </summary>
    [Property]
    public Property ReceiptBilling_AmountMatchesReimbursableLogic()
    {
        return Prop.ForAll(
            Arb.From(Generators.ReceiptGen),
            receipt =>
            {
                var receipts = new List<Receipt> { receipt };
                var section = BillingReportService.BuildReceiptSection(receipts);

                var expectedAmount = receipt.IsReimbursable ? receipt.Amount : 0m;

                return section.LineItems.Count == 1
                    && section.LineItems[0].Amount == expectedAmount;
            });
    }
}
