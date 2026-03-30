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
/// Bug Condition Exploration — Property 1: Worker-Specific Rate Used in Billing.
/// These tests MUST FAIL on unfixed code, confirming the bug exists.
/// The billing report ignores per-worker rate overrides and always uses the default rate.
/// **Validates: Requirements 1.1, 1.2, 2.1, 2.2**
/// </summary>
public class BillingOverrideExplorationTests
{
    // ── Reflection helpers to call private static methods on BillingReportService ──

    private static readonly MethodInfo BuildWorkSectionMethod =
        typeof(BillingReportService).GetMethod(
            "BuildWorkSection",
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo BuildLaundrySectionMethod =
        typeof(BillingReportService).GetMethod(
            "BuildLaundrySection",
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo BuildMowingSectionMethod =
        typeof(BillingReportService).GetMethod(
            "BuildMowingSection",
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static BillingSection InvokeBuildWorkSection(
        string sectionName, WorkCategory category,
        List<WorkEntry> allEntries, List<Rate> rates,
        Dictionary<Guid, string> houseMap)
    {
        return (BillingSection)BuildWorkSectionMethod.Invoke(
            null, new object[] { sectionName, category, allEntries, rates, houseMap })!;
    }

    private static BillingSection InvokeBuildLaundrySection(
        List<WorkEntry> allEntries, List<Rate> rates,
        Dictionary<Guid, string> houseMap)
    {
        return (BillingSection)BuildLaundrySectionMethod.Invoke(
            null, new object[] { allEntries, rates, houseMap })!;
    }

    private static BillingSection InvokeBuildMowingSection(
        List<WorkEntry> allEntries, List<LawnHouseRate> lawnRates,
        Dictionary<Guid, string> houseMap)
    {
        return (BillingSection)BuildMowingSectionMethod.Invoke(
            null, new object[] { allEntries, lawnRates, houseMap })!;
    }

    // ── Generators that produce a worker entry + worker override + distinct default rate ──

    /// <summary>
    /// Generates two distinct positive decimals (for worker override vs default rate).
    /// </summary>
    private static Gen<(decimal workerRate, decimal defaultRate)> DistinctRatePairGen =>
        from r1 in Generators.PositiveDecimalGen
        from r2 in Generators.PositiveDecimalGen.Where(r => r != r1)
        select (r1, r2);

    // ── Property 1a: Cleaning entries use worker-specific RateCharged ──

    /// <summary>
    /// Property 1a: For a Cleaning entry where the worker has a per-worker rate override,
    /// the billing amount MUST equal hours × workerOverride.RateCharged.
    /// On unfixed code this FAILS because billing always uses the default rate.
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Property]
    public Property CleaningEntry_UsesWorkerOverrideRate()
    {
        return Prop.ForAll(
            Arb.From(Generators.CleaningEntryGen),
            Arb.From(DistinctRatePairGen),
            (entry, rates) =>
            {
                var (workerRateCharged, defaultRateCharged) = rates;
                var workerId = entry.WorkerId;
                var houseId = entry.HouseId;

                var workerRate = new Rate
                {
                    Id = Guid.NewGuid(),
                    WorkCategoryValue = WorkCategory.Cleaning.ToString(),
                    WorkerId = workerId,
                    RateCharged = workerRateCharged,
                    RatePaid = 10m,
                    UpdatedAt = DateTime.UtcNow
                };
                var defaultRate = new Rate
                {
                    Id = Guid.NewGuid(),
                    WorkCategoryValue = WorkCategory.Cleaning.ToString(),
                    WorkerId = null,
                    RateCharged = defaultRateCharged,
                    RatePaid = 10m,
                    UpdatedAt = DateTime.UtcNow
                };

                var ratesList = new List<Rate> { workerRate, defaultRate };
                var houseMap = new Dictionary<Guid, string> { { houseId, "Test House" } };
                var allEntries = new List<WorkEntry> { entry };

                var section = InvokeBuildWorkSection("Cleaning", WorkCategory.Cleaning, allEntries, ratesList, houseMap);

                var expectedAmount = (entry.HoursBilled ?? 0) * workerRateCharged;
                var actualAmount = section.LineItems.Sum(li => li.Amount);

                return actualAmount == expectedAmount;
            });
    }

    // ── Property 1b: Maintenance entries use worker-specific RateCharged ──

    /// <summary>
    /// Property 1b: For a Maintenance entry where the worker has a per-worker rate override,
    /// the billing amount MUST equal hours × workerOverride.RateCharged.
    /// On unfixed code this FAILS because billing always uses the default rate.
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Property]
    public Property MaintenanceEntry_UsesWorkerOverrideRate()
    {
        return Prop.ForAll(
            Arb.From(Generators.MaintenanceEntryGen),
            Arb.From(DistinctRatePairGen),
            (entry, rates) =>
            {
                var (workerRateCharged, defaultRateCharged) = rates;
                var workerId = entry.WorkerId;
                var houseId = entry.HouseId;

                var workerRate = new Rate
                {
                    Id = Guid.NewGuid(),
                    WorkCategoryValue = WorkCategory.Maintenance.ToString(),
                    WorkerId = workerId,
                    RateCharged = workerRateCharged,
                    RatePaid = 10m,
                    UpdatedAt = DateTime.UtcNow
                };
                var defaultRate = new Rate
                {
                    Id = Guid.NewGuid(),
                    WorkCategoryValue = WorkCategory.Maintenance.ToString(),
                    WorkerId = null,
                    RateCharged = defaultRateCharged,
                    RatePaid = 10m,
                    UpdatedAt = DateTime.UtcNow
                };

                var ratesList = new List<Rate> { workerRate, defaultRate };
                var houseMap = new Dictionary<Guid, string> { { houseId, "Test House" } };
                var allEntries = new List<WorkEntry> { entry };

                var section = InvokeBuildWorkSection("Maintenance", WorkCategory.Maintenance, allEntries, ratesList, houseMap);

                var expectedAmount = (entry.HoursBilled ?? 0) * workerRateCharged;
                var actualAmount = section.LineItems.Sum(li => li.Amount);

                return actualAmount == expectedAmount;
            });
    }

    // ── Property 1c: Laundry entries use worker-specific RateCharged ──

    /// <summary>
    /// Property 1c: For a Laundry entry where the worker has a per-worker rate override,
    /// the billing amount MUST equal loads × workerOverride.RateCharged.
    /// On unfixed code this FAILS because billing always uses the default rate.
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Property]
    public Property LaundryEntry_UsesWorkerOverrideRate()
    {
        return Prop.ForAll(
            Arb.From(Generators.LaundryEntryGen),
            Arb.From(DistinctRatePairGen),
            (entry, rates) =>
            {
                var (workerRateCharged, defaultRateCharged) = rates;
                var workerId = entry.WorkerId;
                var houseId = entry.HouseId;

                var workerRate = new Rate
                {
                    Id = Guid.NewGuid(),
                    WorkCategoryValue = WorkCategory.Laundry.ToString(),
                    WorkerId = workerId,
                    RateCharged = workerRateCharged,
                    RatePaid = 10m,
                    UpdatedAt = DateTime.UtcNow
                };
                var defaultRate = new Rate
                {
                    Id = Guid.NewGuid(),
                    WorkCategoryValue = WorkCategory.Laundry.ToString(),
                    WorkerId = null,
                    RateCharged = defaultRateCharged,
                    RatePaid = 10m,
                    UpdatedAt = DateTime.UtcNow
                };

                var ratesList = new List<Rate> { workerRate, defaultRate };
                var houseMap = new Dictionary<Guid, string> { { houseId, "Test House" } };
                var allEntries = new List<WorkEntry> { entry };

                var section = InvokeBuildLaundrySection(allEntries, ratesList, houseMap);

                var expectedAmount = (entry.NumberOfLoads ?? 0) * workerRateCharged;
                var actualAmount = section.LineItems.Sum(li => li.Amount);

                return actualAmount == expectedAmount;
            });
    }

    // ── Property 1d: Mowing entries use worker-specific LawnHouseRate RateCharged ──

    /// <summary>
    /// Property 1d: For a Lawn entry where the worker has a per-worker lawn rate override
    /// for that house, the billing amount MUST equal workerOverride.RateCharged.
    /// On unfixed code this FAILS because billing always uses the default lawn rate.
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Property]
    public Property MowingEntry_UsesWorkerOverrideLawnRate()
    {
        return Prop.ForAll(
            Arb.From(Generators.LawnEntryGen),
            Arb.From(DistinctRatePairGen),
            (entry, rates) =>
            {
                var (workerRateCharged, defaultRateCharged) = rates;
                var workerId = entry.WorkerId;
                var houseId = entry.HouseId;

                var workerLawnRate = new LawnHouseRate
                {
                    Id = Guid.NewGuid(),
                    HouseId = houseId,
                    WorkerId = workerId,
                    RateCharged = workerRateCharged,
                    RatePaid = 10m,
                    UpdatedAt = DateTime.UtcNow
                };
                var defaultLawnRate = new LawnHouseRate
                {
                    Id = Guid.NewGuid(),
                    HouseId = houseId,
                    WorkerId = null,
                    RateCharged = defaultRateCharged,
                    RatePaid = 10m,
                    UpdatedAt = DateTime.UtcNow
                };

                var lawnRates = new List<LawnHouseRate> { workerLawnRate, defaultLawnRate };
                var houseMap = new Dictionary<Guid, string> { { houseId, "Test House" } };
                var allEntries = new List<WorkEntry> { entry };

                var section = InvokeBuildMowingSection(allEntries, lawnRates, houseMap);

                var expectedAmount = workerRateCharged;
                var actualAmount = section.LineItems.Sum(li => li.Amount);

                return actualAmount == expectedAmount;
            });
    }

    // ── Property 1e: ValidateRate rejects $0.00 for default rates (confirms $0.00 block exists) ──

    /// <summary>
    /// Property 1e: RateService.ValidateRate(0m, paid) throws ArgumentException for any
    /// non-negative paid value. This confirms the $0.00 block exists in current code.
    /// This test SHOULD PASS on unfixed code (the block is the current behavior).
    /// **Validates: Requirements 1.3, 1.4**
    /// </summary>
    [Property]
    public Property ValidateRate_RejectsZeroChargedForDefaultRates()
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
                    return true; // Expected: $0.00 is rejected
                }
            });
    }
}
