using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Services;
using AuntiesCleaners.Tests.Helpers;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using NSubstitute;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// Property-based tests for the BossProfitReport model arithmetic.
/// Feature: boss-profit-report
/// </summary>
public class BossProfitReportTests
{
    private static Gen<decimal> AmountGen =>
        Gen.Choose(-100000, 100000).Select(i => (decimal)i / 100m);

    private static Gen<ProfitCategoryBreakdown> CategoryBreakdownGen =>
        from name in Gen.Elements("Cleaning", "Laundry", "Maintenance", "Lawn", "Miscellaneous")
        from bossOwn in AmountGen
        from workerMargin in AmountGen
        select new ProfitCategoryBreakdown
        {
            CategoryName = name,
            BossOwnTotal = bossOwn,
            WorkerMarginTotal = workerMargin
        };

    private static Gen<BossProfitReport> BossProfitReportGen =>
        from categories in Gen.ListOf(CategoryBreakdownGen)
        select new BossProfitReport
        {
            DateFrom = DateTime.Today.AddDays(-30),
            DateTo = DateTime.Today,
            Categories = categories.ToList()
        };

    /// <summary>
    /// Feature: boss-profit-report, Property 5: Grand total equals sum of all category breakdowns
    /// Verify GrandTotal == sum(BossOwnTotal + WorkerMarginTotal) across all categories.
    /// **Validates: Requirements 6.3, 7.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property GrandTotalEqualsSumOfCategories()
    {
        return Prop.ForAll(
            Arb.From(BossProfitReportGen),
            report =>
            {
                var expected = report.Categories.Sum(c => c.BossOwnTotal + c.WorkerMarginTotal);
                return report.GrandTotal == expected;
            });
    }

    /// <summary>
    /// Feature: boss-profit-report, Property 6: Report always contains exactly five categories
    /// Generate random inputs and invoke the service. Verify the Categories list always has
    /// exactly 5 entries: Cleaning, Laundry, Maintenance, Lawn, Miscellaneous.
    /// **Validates: Requirements 7.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ReportAlwaysHasFiveCategories()
    {
        var expectedCategories = new HashSet<string> { "Cleaning", "Laundry", "Maintenance", "Lawn", "Miscellaneous" };

        var scenarioGen =
            from bossWorkerId in Gen.OneOf(
                Generators.GuidGen.Select(g => (Guid?)g),
                Gen.Constant((Guid?)null))
            from workEntries in Gen.ListOf(Generators.WorkEntryGen)
            from miscEntries in Gen.ListOf(Generators.MiscellaneousEntryGen)
            from cleaningRates in Gen.ListOf(Generators.RateGen)
            from laundryRates in Gen.ListOf(Generators.RateGen)
            from maintenanceRates in Gen.ListOf(Generators.RateGen)
            from lawnRates in Gen.ListOf(Generators.LawnHouseRateGen)
            select new
            {
                BossWorkerId = bossWorkerId,
                WorkEntries = workEntries.ToList(),
                MiscEntries = miscEntries.ToList(),
                CleaningRates = cleaningRates.ToList(),
                LaundryRates = laundryRates.ToList(),
                MaintenanceRates = maintenanceRates.ToList(),
                LawnRates = lawnRates.ToList()
            };

        return Prop.ForAll(
            Arb.From(scenarioGen),
            scenario =>
            {
                var authService = Substitute.For<IAuthService>();
                authService.GetCurrentWorkerIdAsync().Returns(Task.FromResult(scenario.BossWorkerId));

                var workEntryService = Substitute.For<IWorkEntryService>();
                workEntryService.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(scenario.WorkEntries));

                var miscEntryService = Substitute.For<IMiscEntryService>();
                miscEntryService.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(scenario.MiscEntries));

                var rateService = Substitute.For<IRateService>();
                rateService.GetByCategoryAsync("Cleaning").Returns(Task.FromResult(scenario.CleaningRates));
                rateService.GetByCategoryAsync("Laundry").Returns(Task.FromResult(scenario.LaundryRates));
                rateService.GetByCategoryAsync("Maintenance").Returns(Task.FromResult(scenario.MaintenanceRates));
                rateService.GetAllLawnRatesAsync().Returns(Task.FromResult(scenario.LawnRates));

                var service = new ProfitReportService(workEntryService, miscEntryService, rateService, authService);
                var report = service.GenerateReportAsync(DateTime.Today.AddDays(-30), DateTime.Today).Result;

                var actualNames = report.Categories.Select(c => c.CategoryName).ToHashSet();

                return (report.Categories.Count == 5)
                    .Label("Expected exactly 5 categories")
                    .And(actualNames.SetEquals(expectedCategories))
                    .Label("Expected categories: Cleaning, Laundry, Maintenance, Lawn, Miscellaneous");
            });
    }

    /// <summary>
    /// Feature: boss-profit-report, Property 1: Boss own-work profit formula
    /// For any set of work entries belonging to the Boss worker, the Boss own-work profit for each category equals:
    /// Cleaning/Maintenance: sum(HoursBilled × RateCharged), Laundry: sum(NumberOfLoads × RateCharged),
    /// Lawn: sum(LawnHouseRate.RateCharged).
    /// **Validates: Requirements 3.2, 3.3, 3.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property BossOwnWorkProfitFormula()
    {
        var scenarioGen =
            from bossId in Generators.GuidGen
            // Generate boss cleaning entries
            from cleaningEntries in Gen.ListOf(
                from entry in Generators.CleaningEntryGen
                select new WorkEntry
                {
                    Id = entry.Id, WorkerId = bossId, HouseId = entry.HouseId,
                    WorkCategoryValue = entry.WorkCategoryValue, EntryDate = entry.EntryDate,
                    HoursBilled = entry.HoursBilled, NumberOfLoads = entry.NumberOfLoads,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            // Generate boss maintenance entries
            from maintenanceEntries in Gen.ListOf(
                from entry in Generators.MaintenanceEntryGen
                select new WorkEntry
                {
                    Id = entry.Id, WorkerId = bossId, HouseId = entry.HouseId,
                    WorkCategoryValue = entry.WorkCategoryValue, EntryDate = entry.EntryDate,
                    HoursBilled = entry.HoursBilled, NumberOfLoads = entry.NumberOfLoads,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            // Generate boss laundry entries
            from laundryEntries in Gen.ListOf(
                from entry in Generators.LaundryEntryGen
                select new WorkEntry
                {
                    Id = entry.Id, WorkerId = bossId, HouseId = entry.HouseId,
                    WorkCategoryValue = entry.WorkCategoryValue, EntryDate = entry.EntryDate,
                    HoursBilled = entry.HoursBilled, NumberOfLoads = entry.NumberOfLoads,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            // Generate boss lawn entries with known house IDs (paired together)
            from lawnPairs in Gen.ListOf(
                from entry in Generators.LawnEntryGen
                from houseId in Generators.GuidGen
                from rateCharged in Generators.PositiveDecimalGen
                select new { Entry = entry, HouseId = houseId, RateCharged = rateCharged })
            // Generate rates
            from cleaningRateCharged in Generators.PositiveDecimalGen
            from laundryRateCharged in Generators.PositiveDecimalGen
            from maintenanceRateCharged in Generators.PositiveDecimalGen
            select new
            {
                BossId = bossId,
                CleaningEntries = cleaningEntries.ToList(),
                MaintenanceEntries = maintenanceEntries.ToList(),
                LaundryEntries = laundryEntries.ToList(),
                LawnEntries = lawnPairs.Select(p => new WorkEntry
                {
                    Id = p.Entry.Id, WorkerId = bossId, HouseId = p.HouseId,
                    WorkCategoryValue = p.Entry.WorkCategoryValue, EntryDate = p.Entry.EntryDate,
                    HoursBilled = p.Entry.HoursBilled, NumberOfLoads = p.Entry.NumberOfLoads,
                    CreatedBy = p.Entry.CreatedBy, CreatedAt = p.Entry.CreatedAt
                }).ToList(),
                CleaningRateCharged = cleaningRateCharged,
                LaundryRateCharged = laundryRateCharged,
                MaintenanceRateCharged = maintenanceRateCharged,
                LawnPairs = lawnPairs.ToList()
            };

        return Prop.ForAll(
            Arb.From(scenarioGen),
            scenario =>
            {
                // Combine all boss work entries
                var allWorkEntries = new List<WorkEntry>();
                allWorkEntries.AddRange(scenario.CleaningEntries);
                allWorkEntries.AddRange(scenario.MaintenanceEntries);
                allWorkEntries.AddRange(scenario.LaundryEntries);
                allWorkEntries.AddRange(scenario.LawnEntries);

                // Build boss-specific rates per category
                var cleaningRates = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Cleaning", WorkerId = scenario.BossId, RateCharged = scenario.CleaningRateCharged, RatePaid = 0 }
                };
                var laundryRates = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Laundry", WorkerId = scenario.BossId, RateCharged = scenario.LaundryRateCharged, RatePaid = 0 }
                };
                var maintenanceRates = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Maintenance", WorkerId = scenario.BossId, RateCharged = scenario.MaintenanceRateCharged, RatePaid = 0 }
                };

                // Build lawn house rates per entry's house
                var lawnRates = new List<LawnHouseRate>();
                foreach (var pair in scenario.LawnPairs)
                {
                    lawnRates.Add(new LawnHouseRate
                    {
                        Id = Guid.NewGuid(), HouseId = pair.HouseId,
                        WorkerId = scenario.BossId, RateCharged = pair.RateCharged, RatePaid = 0
                    });
                }

                // Set up mocks
                var authService = Substitute.For<IAuthService>();
                authService.GetCurrentWorkerIdAsync().Returns(Task.FromResult((Guid?)scenario.BossId));

                var workEntryService = Substitute.For<IWorkEntryService>();
                workEntryService.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(allWorkEntries));

                var miscEntryService = Substitute.For<IMiscEntryService>();
                miscEntryService.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(new List<MiscellaneousEntry>()));

                var rateService = Substitute.For<IRateService>();
                rateService.GetByCategoryAsync("Cleaning").Returns(Task.FromResult(cleaningRates));
                rateService.GetByCategoryAsync("Laundry").Returns(Task.FromResult(laundryRates));
                rateService.GetByCategoryAsync("Maintenance").Returns(Task.FromResult(maintenanceRates));
                rateService.GetAllLawnRatesAsync().Returns(Task.FromResult(lawnRates));

                var service = new ProfitReportService(workEntryService, miscEntryService, rateService, authService);
                var report = service.GenerateReportAsync(DateTime.Today.AddDays(-30), DateTime.Today).Result;

                // Compute expected values using the formula
                var expectedCleaning = scenario.CleaningEntries.Sum(e => (e.HoursBilled ?? 0) * scenario.CleaningRateCharged);
                var expectedMaintenance = scenario.MaintenanceEntries.Sum(e => (e.HoursBilled ?? 0) * scenario.MaintenanceRateCharged);
                var expectedLaundry = scenario.LaundryEntries.Sum(e => (e.NumberOfLoads ?? 0) * scenario.LaundryRateCharged);
                var expectedLawn = scenario.LawnPairs.Sum(p => p.RateCharged);

                var actualCleaning = report.Categories.First(c => c.CategoryName == "Cleaning").BossOwnTotal;
                var actualLaundry = report.Categories.First(c => c.CategoryName == "Laundry").BossOwnTotal;
                var actualMaintenance = report.Categories.First(c => c.CategoryName == "Maintenance").BossOwnTotal;
                var actualLawn = report.Categories.First(c => c.CategoryName == "Lawn").BossOwnTotal;

                return (actualCleaning == expectedCleaning)
                    .Label($"Cleaning: expected {expectedCleaning}, got {actualCleaning}")
                    .And(actualMaintenance == expectedMaintenance)
                    .Label($"Maintenance: expected {expectedMaintenance}, got {actualMaintenance}")
                    .And(actualLaundry == expectedLaundry)
                    .Label($"Laundry: expected {expectedLaundry}, got {actualLaundry}")
                    .And(actualLawn == expectedLawn)
                    .Label($"Lawn: expected {expectedLawn}, got {actualLawn}");
            });
    }

    /// <summary>
    /// Feature: boss-profit-report, Property 3: Miscellaneous profit is charge minus pay regardless of worker
    /// Generate random misc entries with varying WorkerIds. Verify profit = ChargeAmount - PayAmount for each,
    /// with Boss entries in BossOwnTotal and non-Boss in WorkerMarginTotal.
    /// **Validates: Requirements 3.6, 4.6**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property MiscProfitIsChargeMinusPay()
    {
        var scenarioGen =
            from bossId in Generators.GuidGen
            from workerId in Generators.GuidGen
            // Generate misc entries assigned to the Boss
            from bossMiscEntries in Gen.ListOf(
                from entry in Generators.MiscellaneousEntryGen
                select new MiscellaneousEntry
                {
                    Id = entry.Id, WorkerId = bossId, HouseId = entry.HouseId,
                    EntryDate = entry.EntryDate, Description = entry.Description,
                    ChargeAmount = entry.ChargeAmount, PayAmount = entry.PayAmount,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            // Generate misc entries assigned to a non-Boss worker
            from workerMiscEntries in Gen.ListOf(
                from entry in Generators.MiscellaneousEntryGen
                select new MiscellaneousEntry
                {
                    Id = entry.Id, WorkerId = workerId, HouseId = entry.HouseId,
                    EntryDate = entry.EntryDate, Description = entry.Description,
                    ChargeAmount = entry.ChargeAmount, PayAmount = entry.PayAmount,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            select new
            {
                BossId = bossId,
                WorkerId = workerId,
                BossMiscEntries = bossMiscEntries.ToList(),
                WorkerMiscEntries = workerMiscEntries.ToList()
            };

        return Prop.ForAll(
            Arb.From(scenarioGen),
            scenario =>
            {
                var allMiscEntries = new List<MiscellaneousEntry>();
                allMiscEntries.AddRange(scenario.BossMiscEntries);
                allMiscEntries.AddRange(scenario.WorkerMiscEntries);

                var authService = Substitute.For<IAuthService>();
                authService.GetCurrentWorkerIdAsync().Returns(Task.FromResult((Guid?)scenario.BossId));

                var workEntryService = Substitute.For<IWorkEntryService>();
                workEntryService.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(new List<WorkEntry>()));

                var miscEntryService = Substitute.For<IMiscEntryService>();
                miscEntryService.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(allMiscEntries));

                var rateService = Substitute.For<IRateService>();
                rateService.GetByCategoryAsync("Cleaning").Returns(Task.FromResult(new List<Rate>()));
                rateService.GetByCategoryAsync("Laundry").Returns(Task.FromResult(new List<Rate>()));
                rateService.GetByCategoryAsync("Maintenance").Returns(Task.FromResult(new List<Rate>()));
                rateService.GetAllLawnRatesAsync().Returns(Task.FromResult(new List<LawnHouseRate>()));

                var service = new ProfitReportService(workEntryService, miscEntryService, rateService, authService);
                var report = service.GenerateReportAsync(DateTime.Today.AddDays(-30), DateTime.Today).Result;

                var expectedBossOwn = scenario.BossMiscEntries.Sum(e => e.ChargeAmount - e.PayAmount);
                var expectedWorkerMargin = scenario.WorkerMiscEntries.Sum(e => e.ChargeAmount - e.PayAmount);

                var miscCategory = report.Categories.First(c => c.CategoryName == "Miscellaneous");

                return (miscCategory.BossOwnTotal == expectedBossOwn)
                    .Label($"Boss own misc: expected {expectedBossOwn}, got {miscCategory.BossOwnTotal}")
                    .And(miscCategory.WorkerMarginTotal == expectedWorkerMargin)
                    .Label($"Worker margin misc: expected {expectedWorkerMargin}, got {miscCategory.WorkerMarginTotal}");
            });
    }

    /// <summary>
    /// Feature: boss-profit-report, Property 2: Worker margin profit formula
    /// For any set of work entries belonging to non-Boss workers, the worker margin for each category equals:
    /// Cleaning/Maintenance: sum(HoursBilled × (RateCharged - RatePaid)),
    /// Laundry: sum(NumberOfLoads × (RateCharged - RatePaid)),
    /// Lawn: sum(RateCharged - RatePaid).
    /// **Validates: Requirements 4.1, 4.2, 4.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property WorkerMarginFormula()
    {
        var scenarioGen =
            from bossId in Generators.GuidGen
            from workerId in Generators.GuidGen
            // Generate worker cleaning entries
            from cleaningEntries in Gen.ListOf(
                from entry in Generators.CleaningEntryGen
                select new WorkEntry
                {
                    Id = entry.Id, WorkerId = workerId, HouseId = entry.HouseId,
                    WorkCategoryValue = entry.WorkCategoryValue, EntryDate = entry.EntryDate,
                    HoursBilled = entry.HoursBilled, NumberOfLoads = entry.NumberOfLoads,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            // Generate worker maintenance entries
            from maintenanceEntries in Gen.ListOf(
                from entry in Generators.MaintenanceEntryGen
                select new WorkEntry
                {
                    Id = entry.Id, WorkerId = workerId, HouseId = entry.HouseId,
                    WorkCategoryValue = entry.WorkCategoryValue, EntryDate = entry.EntryDate,
                    HoursBilled = entry.HoursBilled, NumberOfLoads = entry.NumberOfLoads,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            // Generate worker laundry entries
            from laundryEntries in Gen.ListOf(
                from entry in Generators.LaundryEntryGen
                select new WorkEntry
                {
                    Id = entry.Id, WorkerId = workerId, HouseId = entry.HouseId,
                    WorkCategoryValue = entry.WorkCategoryValue, EntryDate = entry.EntryDate,
                    HoursBilled = entry.HoursBilled, NumberOfLoads = entry.NumberOfLoads,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            // Generate worker lawn entries with known house IDs (paired together)
            from lawnPairs in Gen.ListOf(
                from entry in Generators.LawnEntryGen
                from houseId in Generators.GuidGen
                from rateCharged in Generators.PositiveDecimalGen
                from ratePaid in Generators.NonNegativeDecimalGen
                select new { Entry = entry, HouseId = houseId, RateCharged = rateCharged, RatePaid = ratePaid })
            // Generate rates with both charged and paid
            from cleaningRateCharged in Generators.PositiveDecimalGen
            from cleaningRatePaid in Generators.NonNegativeDecimalGen
            from laundryRateCharged in Generators.PositiveDecimalGen
            from laundryRatePaid in Generators.NonNegativeDecimalGen
            from maintenanceRateCharged in Generators.PositiveDecimalGen
            from maintenanceRatePaid in Generators.NonNegativeDecimalGen
            select new
            {
                BossId = bossId,
                WorkerId = workerId,
                CleaningEntries = cleaningEntries.ToList(),
                MaintenanceEntries = maintenanceEntries.ToList(),
                LaundryEntries = laundryEntries.ToList(),
                LawnEntries = lawnPairs.Select(p => new WorkEntry
                {
                    Id = p.Entry.Id, WorkerId = workerId, HouseId = p.HouseId,
                    WorkCategoryValue = p.Entry.WorkCategoryValue, EntryDate = p.Entry.EntryDate,
                    HoursBilled = p.Entry.HoursBilled, NumberOfLoads = p.Entry.NumberOfLoads,
                    CreatedBy = p.Entry.CreatedBy, CreatedAt = p.Entry.CreatedAt
                }).ToList(),
                CleaningRateCharged = cleaningRateCharged,
                CleaningRatePaid = cleaningRatePaid,
                LaundryRateCharged = laundryRateCharged,
                LaundryRatePaid = laundryRatePaid,
                MaintenanceRateCharged = maintenanceRateCharged,
                MaintenanceRatePaid = maintenanceRatePaid,
                LawnPairs = lawnPairs.ToList()
            };

        return Prop.ForAll(
            Arb.From(scenarioGen),
            scenario =>
            {
                // Combine all worker entries (non-Boss)
                var allWorkEntries = new List<WorkEntry>();
                allWorkEntries.AddRange(scenario.CleaningEntries);
                allWorkEntries.AddRange(scenario.MaintenanceEntries);
                allWorkEntries.AddRange(scenario.LaundryEntries);
                allWorkEntries.AddRange(scenario.LawnEntries);

                // Build worker-specific rates per category (with both RateCharged and RatePaid)
                var cleaningRates = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Cleaning", WorkerId = scenario.WorkerId, RateCharged = scenario.CleaningRateCharged, RatePaid = scenario.CleaningRatePaid }
                };
                var laundryRates = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Laundry", WorkerId = scenario.WorkerId, RateCharged = scenario.LaundryRateCharged, RatePaid = scenario.LaundryRatePaid }
                };
                var maintenanceRates = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Maintenance", WorkerId = scenario.WorkerId, RateCharged = scenario.MaintenanceRateCharged, RatePaid = scenario.MaintenanceRatePaid }
                };

                // Build lawn house rates per entry's house (with both RateCharged and RatePaid)
                var lawnRates = new List<LawnHouseRate>();
                foreach (var pair in scenario.LawnPairs)
                {
                    lawnRates.Add(new LawnHouseRate
                    {
                        Id = Guid.NewGuid(), HouseId = pair.HouseId,
                        WorkerId = scenario.WorkerId, RateCharged = pair.RateCharged, RatePaid = pair.RatePaid
                    });
                }

                // Set up mocks — Boss is a different worker, so all entries are "other worker"
                var authService = Substitute.For<IAuthService>();
                authService.GetCurrentWorkerIdAsync().Returns(Task.FromResult((Guid?)scenario.BossId));

                var workEntryService = Substitute.For<IWorkEntryService>();
                workEntryService.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(allWorkEntries));

                var miscEntryService = Substitute.For<IMiscEntryService>();
                miscEntryService.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(new List<MiscellaneousEntry>()));

                var rateService = Substitute.For<IRateService>();
                rateService.GetByCategoryAsync("Cleaning").Returns(Task.FromResult(cleaningRates));
                rateService.GetByCategoryAsync("Laundry").Returns(Task.FromResult(laundryRates));
                rateService.GetByCategoryAsync("Maintenance").Returns(Task.FromResult(maintenanceRates));
                rateService.GetAllLawnRatesAsync().Returns(Task.FromResult(lawnRates));

                var service = new ProfitReportService(workEntryService, miscEntryService, rateService, authService);
                var report = service.GenerateReportAsync(DateTime.Today.AddDays(-30), DateTime.Today).Result;

                // Compute expected worker margin values using the formula
                var expectedCleaning = scenario.CleaningEntries.Sum(e => (e.HoursBilled ?? 0) * (scenario.CleaningRateCharged - scenario.CleaningRatePaid));
                var expectedMaintenance = scenario.MaintenanceEntries.Sum(e => (e.HoursBilled ?? 0) * (scenario.MaintenanceRateCharged - scenario.MaintenanceRatePaid));
                var expectedLaundry = scenario.LaundryEntries.Sum(e => (e.NumberOfLoads ?? 0) * (scenario.LaundryRateCharged - scenario.LaundryRatePaid));
                var expectedLawn = scenario.LawnPairs.Sum(p => p.RateCharged - p.RatePaid);

                var actualCleaning = report.Categories.First(c => c.CategoryName == "Cleaning").WorkerMarginTotal;
                var actualLaundry = report.Categories.First(c => c.CategoryName == "Laundry").WorkerMarginTotal;
                var actualMaintenance = report.Categories.First(c => c.CategoryName == "Maintenance").WorkerMarginTotal;
                var actualLawn = report.Categories.First(c => c.CategoryName == "Lawn").WorkerMarginTotal;

                return (actualCleaning == expectedCleaning)
                    .Label($"Cleaning margin: expected {expectedCleaning}, got {actualCleaning}")
                    .And(actualMaintenance == expectedMaintenance)
                    .Label($"Maintenance margin: expected {expectedMaintenance}, got {actualMaintenance}")
                    .And(actualLaundry == expectedLaundry)
                    .Label($"Laundry margin: expected {expectedLaundry}, got {actualLaundry}")
                    .And(actualLawn == expectedLawn)
                    .Label($"Lawn margin: expected {expectedLawn}, got {actualLawn}");
            });
    }

    /// <summary>
    /// Feature: boss-profit-report, Property 4: Rate resolution prefers worker-specific over default
    /// When both worker-specific and default rates exist, the worker-specific rate is used.
    /// When only a default rate exists, the default rate is used. Same for LawnHouseRate.
    /// **Validates: Requirements 3.5, 4.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RateResolutionPrefersWorkerSpecific()
    {
        var scenarioGen =
            from bossId in Generators.GuidGen
            from workerId in Generators.GuidGen
            from houseId in Generators.GuidGen
            // Worker-specific cleaning rates
            from workerCleaningCharged in Generators.PositiveDecimalGen
            from workerCleaningPaid in Generators.NonNegativeDecimalGen
            // Default cleaning rates (different values)
            from defaultCleaningCharged in Generators.PositiveDecimalGen
            from defaultCleaningPaid in Generators.NonNegativeDecimalGen
            // Worker-specific lawn rates
            from workerLawnCharged in Generators.PositiveDecimalGen
            from workerLawnPaid in Generators.NonNegativeDecimalGen
            // Default lawn rates (different values)
            from defaultLawnCharged in Generators.PositiveDecimalGen
            from defaultLawnPaid in Generators.NonNegativeDecimalGen
            // Entries
            from cleaningEntry in Generators.CleaningEntryGen
            from lawnEntry in Generators.LawnEntryGen
            select new
            {
                BossId = bossId,
                WorkerId = workerId,
                HouseId = houseId,
                WorkerCleaningCharged = workerCleaningCharged,
                WorkerCleaningPaid = workerCleaningPaid,
                DefaultCleaningCharged = defaultCleaningCharged,
                DefaultCleaningPaid = defaultCleaningPaid,
                WorkerLawnCharged = workerLawnCharged,
                WorkerLawnPaid = workerLawnPaid,
                DefaultLawnCharged = defaultLawnCharged,
                DefaultLawnPaid = defaultLawnPaid,
                CleaningEntry = cleaningEntry,
                LawnEntry = lawnEntry
            };

        return Prop.ForAll(
            Arb.From(scenarioGen),
            scenario =>
            {
                // ── Scenario A: Worker-specific rate exists → used over default ──

                var cleaningEntryA = new WorkEntry
                {
                    Id = scenario.CleaningEntry.Id, WorkerId = scenario.WorkerId,
                    HouseId = scenario.CleaningEntry.HouseId,
                    WorkCategoryValue = "Cleaning", EntryDate = scenario.CleaningEntry.EntryDate,
                    HoursBilled = scenario.CleaningEntry.HoursBilled,
                    NumberOfLoads = scenario.CleaningEntry.NumberOfLoads,
                    CreatedBy = scenario.CleaningEntry.CreatedBy, CreatedAt = scenario.CleaningEntry.CreatedAt
                };

                var lawnEntryA = new WorkEntry
                {
                    Id = scenario.LawnEntry.Id, WorkerId = scenario.WorkerId,
                    HouseId = scenario.HouseId,
                    WorkCategoryValue = "Lawn", EntryDate = scenario.LawnEntry.EntryDate,
                    HoursBilled = null, NumberOfLoads = null,
                    CreatedBy = scenario.LawnEntry.CreatedBy, CreatedAt = scenario.LawnEntry.CreatedAt
                };

                // Both worker-specific and default rates for Cleaning
                var cleaningRatesA = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Cleaning", WorkerId = scenario.WorkerId, RateCharged = scenario.WorkerCleaningCharged, RatePaid = scenario.WorkerCleaningPaid },
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Cleaning", WorkerId = null, RateCharged = scenario.DefaultCleaningCharged, RatePaid = scenario.DefaultCleaningPaid }
                };

                // Both worker-specific and default lawn rates
                var lawnRatesA = new List<LawnHouseRate>
                {
                    new LawnHouseRate { Id = Guid.NewGuid(), HouseId = scenario.HouseId, WorkerId = scenario.WorkerId, RateCharged = scenario.WorkerLawnCharged, RatePaid = scenario.WorkerLawnPaid },
                    new LawnHouseRate { Id = Guid.NewGuid(), HouseId = scenario.HouseId, WorkerId = null, RateCharged = scenario.DefaultLawnCharged, RatePaid = scenario.DefaultLawnPaid }
                };

                var authServiceA = Substitute.For<IAuthService>();
                authServiceA.GetCurrentWorkerIdAsync().Returns(Task.FromResult((Guid?)scenario.BossId));

                var workEntryServiceA = Substitute.For<IWorkEntryService>();
                workEntryServiceA.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(new List<WorkEntry> { cleaningEntryA, lawnEntryA }));

                var miscEntryServiceA = Substitute.For<IMiscEntryService>();
                miscEntryServiceA.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(new List<MiscellaneousEntry>()));

                var rateServiceA = Substitute.For<IRateService>();
                rateServiceA.GetByCategoryAsync("Cleaning").Returns(Task.FromResult(cleaningRatesA));
                rateServiceA.GetByCategoryAsync("Laundry").Returns(Task.FromResult(new List<Rate>()));
                rateServiceA.GetByCategoryAsync("Maintenance").Returns(Task.FromResult(new List<Rate>()));
                rateServiceA.GetAllLawnRatesAsync().Returns(Task.FromResult(lawnRatesA));

                var serviceA = new ProfitReportService(workEntryServiceA, miscEntryServiceA, rateServiceA, authServiceA);
                var reportA = serviceA.GenerateReportAsync(DateTime.Today.AddDays(-30), DateTime.Today).Result;

                // Worker-specific rates should be used (worker margin since workerId != bossId)
                var expectedCleaningMarginA = (cleaningEntryA.HoursBilled ?? 0) * (scenario.WorkerCleaningCharged - scenario.WorkerCleaningPaid);
                var expectedLawnMarginA = scenario.WorkerLawnCharged - scenario.WorkerLawnPaid;

                var actualCleaningMarginA = reportA.Categories.First(c => c.CategoryName == "Cleaning").WorkerMarginTotal;
                var actualLawnMarginA = reportA.Categories.First(c => c.CategoryName == "Lawn").WorkerMarginTotal;

                // ── Scenario B: No worker-specific rate → default rate is used ──

                var otherWorkerId = Guid.NewGuid();
                var cleaningEntryB = new WorkEntry
                {
                    Id = Guid.NewGuid(), WorkerId = otherWorkerId,
                    HouseId = scenario.CleaningEntry.HouseId,
                    WorkCategoryValue = "Cleaning", EntryDate = scenario.CleaningEntry.EntryDate,
                    HoursBilled = scenario.CleaningEntry.HoursBilled,
                    NumberOfLoads = scenario.CleaningEntry.NumberOfLoads,
                    CreatedBy = scenario.CleaningEntry.CreatedBy, CreatedAt = scenario.CleaningEntry.CreatedAt
                };

                var lawnEntryB = new WorkEntry
                {
                    Id = Guid.NewGuid(), WorkerId = otherWorkerId,
                    HouseId = scenario.HouseId,
                    WorkCategoryValue = "Lawn", EntryDate = scenario.LawnEntry.EntryDate,
                    HoursBilled = null, NumberOfLoads = null,
                    CreatedBy = scenario.LawnEntry.CreatedBy, CreatedAt = scenario.LawnEntry.CreatedAt
                };

                // Only default rates (no worker-specific for otherWorkerId)
                var cleaningRatesB = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Cleaning", WorkerId = null, RateCharged = scenario.DefaultCleaningCharged, RatePaid = scenario.DefaultCleaningPaid }
                };

                var lawnRatesB = new List<LawnHouseRate>
                {
                    new LawnHouseRate { Id = Guid.NewGuid(), HouseId = scenario.HouseId, WorkerId = null, RateCharged = scenario.DefaultLawnCharged, RatePaid = scenario.DefaultLawnPaid }
                };

                var authServiceB = Substitute.For<IAuthService>();
                authServiceB.GetCurrentWorkerIdAsync().Returns(Task.FromResult((Guid?)scenario.BossId));

                var workEntryServiceB = Substitute.For<IWorkEntryService>();
                workEntryServiceB.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(new List<WorkEntry> { cleaningEntryB, lawnEntryB }));

                var miscEntryServiceB = Substitute.For<IMiscEntryService>();
                miscEntryServiceB.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(new List<MiscellaneousEntry>()));

                var rateServiceB = Substitute.For<IRateService>();
                rateServiceB.GetByCategoryAsync("Cleaning").Returns(Task.FromResult(cleaningRatesB));
                rateServiceB.GetByCategoryAsync("Laundry").Returns(Task.FromResult(new List<Rate>()));
                rateServiceB.GetByCategoryAsync("Maintenance").Returns(Task.FromResult(new List<Rate>()));
                rateServiceB.GetAllLawnRatesAsync().Returns(Task.FromResult(lawnRatesB));

                var serviceB = new ProfitReportService(workEntryServiceB, miscEntryServiceB, rateServiceB, authServiceB);
                var reportB = serviceB.GenerateReportAsync(DateTime.Today.AddDays(-30), DateTime.Today).Result;

                // Default rates should be used
                var expectedCleaningMarginB = (cleaningEntryB.HoursBilled ?? 0) * (scenario.DefaultCleaningCharged - scenario.DefaultCleaningPaid);
                var expectedLawnMarginB = scenario.DefaultLawnCharged - scenario.DefaultLawnPaid;

                var actualCleaningMarginB = reportB.Categories.First(c => c.CategoryName == "Cleaning").WorkerMarginTotal;
                var actualLawnMarginB = reportB.Categories.First(c => c.CategoryName == "Lawn").WorkerMarginTotal;

                return (actualCleaningMarginA == expectedCleaningMarginA)
                    .Label($"Scenario A - Cleaning worker-specific: expected {expectedCleaningMarginA}, got {actualCleaningMarginA}")
                    .And(actualLawnMarginA == expectedLawnMarginA)
                    .Label($"Scenario A - Lawn worker-specific: expected {expectedLawnMarginA}, got {actualLawnMarginA}")
                    .And(actualCleaningMarginB == expectedCleaningMarginB)
                    .Label($"Scenario B - Cleaning default: expected {expectedCleaningMarginB}, got {actualCleaningMarginB}")
                    .And(actualLawnMarginB == expectedLawnMarginB)
                    .Label($"Scenario B - Lawn default: expected {expectedLawnMarginB}, got {actualLawnMarginB}");
            });
    }

    /// <summary>
    /// Feature: boss-profit-report, Property 7: Null Boss WorkerId means zero Boss own-work
    /// Generate random entries with Boss WorkerId = null. Verify all category BossOwnTotal = 0
    /// and all profit appears in WorkerMarginTotal.
    /// **Validates: Requirements 9.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property NullBossWorkerIdMeansZeroBossOwn()
    {
        var scenarioGen =
            from workerId in Generators.GuidGen
            // Generate work entries assigned to a worker
            from cleaningEntries in Gen.ListOf(
                from entry in Generators.CleaningEntryGen
                select new WorkEntry
                {
                    Id = entry.Id, WorkerId = workerId, HouseId = entry.HouseId,
                    WorkCategoryValue = entry.WorkCategoryValue, EntryDate = entry.EntryDate,
                    HoursBilled = entry.HoursBilled, NumberOfLoads = entry.NumberOfLoads,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            from maintenanceEntries in Gen.ListOf(
                from entry in Generators.MaintenanceEntryGen
                select new WorkEntry
                {
                    Id = entry.Id, WorkerId = workerId, HouseId = entry.HouseId,
                    WorkCategoryValue = entry.WorkCategoryValue, EntryDate = entry.EntryDate,
                    HoursBilled = entry.HoursBilled, NumberOfLoads = entry.NumberOfLoads,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            from laundryEntries in Gen.ListOf(
                from entry in Generators.LaundryEntryGen
                select new WorkEntry
                {
                    Id = entry.Id, WorkerId = workerId, HouseId = entry.HouseId,
                    WorkCategoryValue = entry.WorkCategoryValue, EntryDate = entry.EntryDate,
                    HoursBilled = entry.HoursBilled, NumberOfLoads = entry.NumberOfLoads,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            from lawnPairs in Gen.ListOf(
                from entry in Generators.LawnEntryGen
                from houseId in Generators.GuidGen
                from rateCharged in Generators.PositiveDecimalGen
                from ratePaid in Generators.NonNegativeDecimalGen
                select new { Entry = entry, HouseId = houseId, RateCharged = rateCharged, RatePaid = ratePaid })
            from miscEntries in Gen.ListOf(
                from entry in Generators.MiscellaneousEntryGen
                select new MiscellaneousEntry
                {
                    Id = entry.Id, WorkerId = workerId, HouseId = entry.HouseId,
                    EntryDate = entry.EntryDate, Description = entry.Description,
                    ChargeAmount = entry.ChargeAmount, PayAmount = entry.PayAmount,
                    CreatedBy = entry.CreatedBy, CreatedAt = entry.CreatedAt
                })
            // Generate rates for the worker
            from cleaningRateCharged in Generators.PositiveDecimalGen
            from cleaningRatePaid in Generators.NonNegativeDecimalGen
            from laundryRateCharged in Generators.PositiveDecimalGen
            from laundryRatePaid in Generators.NonNegativeDecimalGen
            from maintenanceRateCharged in Generators.PositiveDecimalGen
            from maintenanceRatePaid in Generators.NonNegativeDecimalGen
            select new
            {
                WorkerId = workerId,
                CleaningEntries = cleaningEntries.ToList(),
                MaintenanceEntries = maintenanceEntries.ToList(),
                LaundryEntries = laundryEntries.ToList(),
                LawnEntries = lawnPairs.Select(p => new WorkEntry
                {
                    Id = p.Entry.Id, WorkerId = workerId, HouseId = p.HouseId,
                    WorkCategoryValue = p.Entry.WorkCategoryValue, EntryDate = p.Entry.EntryDate,
                    HoursBilled = p.Entry.HoursBilled, NumberOfLoads = p.Entry.NumberOfLoads,
                    CreatedBy = p.Entry.CreatedBy, CreatedAt = p.Entry.CreatedAt
                }).ToList(),
                MiscEntries = miscEntries.ToList(),
                CleaningRateCharged = cleaningRateCharged,
                CleaningRatePaid = cleaningRatePaid,
                LaundryRateCharged = laundryRateCharged,
                LaundryRatePaid = laundryRatePaid,
                MaintenanceRateCharged = maintenanceRateCharged,
                MaintenanceRatePaid = maintenanceRatePaid,
                LawnPairs = lawnPairs.ToList()
            };

        return Prop.ForAll(
            Arb.From(scenarioGen),
            scenario =>
            {
                var allWorkEntries = new List<WorkEntry>();
                allWorkEntries.AddRange(scenario.CleaningEntries);
                allWorkEntries.AddRange(scenario.MaintenanceEntries);
                allWorkEntries.AddRange(scenario.LaundryEntries);
                allWorkEntries.AddRange(scenario.LawnEntries);

                // Boss WorkerId is null — no boss worker linked
                var authService = Substitute.For<IAuthService>();
                authService.GetCurrentWorkerIdAsync().Returns(Task.FromResult((Guid?)null));

                var workEntryService = Substitute.For<IWorkEntryService>();
                workEntryService.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(allWorkEntries));

                var miscEntryService = Substitute.For<IMiscEntryService>();
                miscEntryService.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .Returns(Task.FromResult(scenario.MiscEntries));

                // Worker-specific rates
                var cleaningRates = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Cleaning", WorkerId = scenario.WorkerId, RateCharged = scenario.CleaningRateCharged, RatePaid = scenario.CleaningRatePaid }
                };
                var laundryRates = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Laundry", WorkerId = scenario.WorkerId, RateCharged = scenario.LaundryRateCharged, RatePaid = scenario.LaundryRatePaid }
                };
                var maintenanceRates = new List<Rate>
                {
                    new Rate { Id = Guid.NewGuid(), WorkCategoryValue = "Maintenance", WorkerId = scenario.WorkerId, RateCharged = scenario.MaintenanceRateCharged, RatePaid = scenario.MaintenanceRatePaid }
                };
                var lawnRates = new List<LawnHouseRate>();
                foreach (var pair in scenario.LawnPairs)
                {
                    lawnRates.Add(new LawnHouseRate
                    {
                        Id = Guid.NewGuid(), HouseId = pair.HouseId,
                        WorkerId = scenario.WorkerId, RateCharged = pair.RateCharged, RatePaid = pair.RatePaid
                    });
                }

                var rateService = Substitute.For<IRateService>();
                rateService.GetByCategoryAsync("Cleaning").Returns(Task.FromResult(cleaningRates));
                rateService.GetByCategoryAsync("Laundry").Returns(Task.FromResult(laundryRates));
                rateService.GetByCategoryAsync("Maintenance").Returns(Task.FromResult(maintenanceRates));
                rateService.GetAllLawnRatesAsync().Returns(Task.FromResult(lawnRates));

                var service = new ProfitReportService(workEntryService, miscEntryService, rateService, authService);
                var report = service.GenerateReportAsync(DateTime.Today.AddDays(-30), DateTime.Today).Result;

                // All BossOwnTotal must be 0 when Boss WorkerId is null
                var allBossOwnZero = report.Categories.All(c => c.BossOwnTotal == 0m);

                // Compute expected worker margin totals
                var expectedCleaningMargin = scenario.CleaningEntries.Sum(e => (e.HoursBilled ?? 0) * (scenario.CleaningRateCharged - scenario.CleaningRatePaid));
                var expectedMaintenanceMargin = scenario.MaintenanceEntries.Sum(e => (e.HoursBilled ?? 0) * (scenario.MaintenanceRateCharged - scenario.MaintenanceRatePaid));
                var expectedLaundryMargin = scenario.LaundryEntries.Sum(e => (e.NumberOfLoads ?? 0) * (scenario.LaundryRateCharged - scenario.LaundryRatePaid));
                var expectedLawnMargin = scenario.LawnPairs.Sum(p => p.RateCharged - p.RatePaid);
                var expectedMiscMargin = scenario.MiscEntries.Sum(e => e.ChargeAmount - e.PayAmount);

                var actualCleaningMargin = report.Categories.First(c => c.CategoryName == "Cleaning").WorkerMarginTotal;
                var actualMaintenanceMargin = report.Categories.First(c => c.CategoryName == "Maintenance").WorkerMarginTotal;
                var actualLaundryMargin = report.Categories.First(c => c.CategoryName == "Laundry").WorkerMarginTotal;
                var actualLawnMargin = report.Categories.First(c => c.CategoryName == "Lawn").WorkerMarginTotal;
                var actualMiscMargin = report.Categories.First(c => c.CategoryName == "Miscellaneous").WorkerMarginTotal;

                return allBossOwnZero
                    .Label("All BossOwnTotal should be 0 when Boss WorkerId is null")
                    .And(actualCleaningMargin == expectedCleaningMargin)
                    .Label($"Cleaning margin: expected {expectedCleaningMargin}, got {actualCleaningMargin}")
                    .And(actualMaintenanceMargin == expectedMaintenanceMargin)
                    .Label($"Maintenance margin: expected {expectedMaintenanceMargin}, got {actualMaintenanceMargin}")
                    .And(actualLaundryMargin == expectedLaundryMargin)
                    .Label($"Laundry margin: expected {expectedLaundryMargin}, got {actualLaundryMargin}")
                    .And(actualLawnMargin == expectedLawnMargin)
                    .Label($"Lawn margin: expected {expectedLawnMargin}, got {actualLawnMargin}")
                    .And(actualMiscMargin == expectedMiscMargin)
                    .Label($"Misc margin: expected {expectedMiscMargin}, got {actualMiscMargin}");
            });
    }


}


