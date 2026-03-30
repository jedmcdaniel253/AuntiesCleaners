using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public class ProfitReportService : IProfitReportService
{
    private readonly IWorkEntryService _workEntryService;
    private readonly IMiscEntryService _miscEntryService;
    private readonly IRateService _rateService;
    private readonly IAuthService _authService;

    public ProfitReportService(
        IWorkEntryService workEntryService,
        IMiscEntryService miscEntryService,
        IRateService rateService,
        IAuthService authService)
    {
        _workEntryService = workEntryService;
        _miscEntryService = miscEntryService;
        _rateService = rateService;
        _authService = authService;
    }

    public async Task<BossProfitReport> GenerateReportAsync(DateTime from, DateTime to)
    {
        var bossWorkerId = await _authService.GetCurrentWorkerIdAsync();

        var workEntries = await _workEntryService.GetByDateRangeAsync(from, to);
        var miscEntries = await _miscEntryService.GetByDateRangeAsync(from, to);

        var cleaningRates = await _rateService.GetByCategoryAsync("Cleaning");
        var laundryRates = await _rateService.GetByCategoryAsync("Laundry");
        var maintenanceRates = await _rateService.GetByCategoryAsync("Maintenance");
        var lawnRates = await _rateService.GetAllLawnRatesAsync();

        var report = new BossProfitReport { DateFrom = from, DateTo = to };

        // Partition work entries into Boss own-work vs. other-worker
        var bossWorkEntries = bossWorkerId.HasValue
            ? workEntries.Where(e => e.WorkerId == bossWorkerId.Value).ToList()
            : new List<WorkEntry>();
        var otherWorkEntries = bossWorkerId.HasValue
            ? workEntries.Where(e => e.WorkerId != bossWorkerId.Value).ToList()
            : workEntries;

        // Partition misc entries
        var bossMiscEntries = bossWorkerId.HasValue
            ? miscEntries.Where(e => e.WorkerId == bossWorkerId.Value).ToList()
            : new List<MiscellaneousEntry>();
        var otherMiscEntries = bossWorkerId.HasValue
            ? miscEntries.Where(e => e.WorkerId != bossWorkerId.Value).ToList()
            : miscEntries;

        // --- Cleaning ---
        var cleaningBreakdown = new ProfitCategoryBreakdown { CategoryName = "Cleaning" };
        cleaningBreakdown.BossOwnTotal = ComputeBossOwnHourly(bossWorkEntries, WorkCategory.Cleaning.ToString(), cleaningRates, bossWorkerId);
        cleaningBreakdown.WorkerMarginTotal = ComputeWorkerMarginHourly(otherWorkEntries, WorkCategory.Cleaning.ToString(), cleaningRates);
        report.Categories.Add(cleaningBreakdown);

        // --- Laundry ---
        var laundryBreakdown = new ProfitCategoryBreakdown { CategoryName = "Laundry" };
        laundryBreakdown.BossOwnTotal = ComputeBossOwnLaundry(bossWorkEntries, laundryRates, bossWorkerId);
        laundryBreakdown.WorkerMarginTotal = ComputeWorkerMarginLaundry(otherWorkEntries, laundryRates);
        report.Categories.Add(laundryBreakdown);

        // --- Maintenance ---
        var maintenanceBreakdown = new ProfitCategoryBreakdown { CategoryName = "Maintenance" };
        maintenanceBreakdown.BossOwnTotal = ComputeBossOwnHourly(bossWorkEntries, WorkCategory.Maintenance.ToString(), maintenanceRates, bossWorkerId);
        maintenanceBreakdown.WorkerMarginTotal = ComputeWorkerMarginHourly(otherWorkEntries, WorkCategory.Maintenance.ToString(), maintenanceRates);
        report.Categories.Add(maintenanceBreakdown);

        // --- Lawn ---
        var lawnBreakdown = new ProfitCategoryBreakdown { CategoryName = "Lawn" };
        lawnBreakdown.BossOwnTotal = ComputeBossOwnLawn(bossWorkEntries, lawnRates, bossWorkerId);
        lawnBreakdown.WorkerMarginTotal = ComputeWorkerMarginLawn(otherWorkEntries, lawnRates);
        report.Categories.Add(lawnBreakdown);

        // --- Miscellaneous ---
        var miscBreakdown = new ProfitCategoryBreakdown { CategoryName = "Miscellaneous" };
        miscBreakdown.BossOwnTotal = bossMiscEntries.Sum(e => e.ChargeAmount - e.PayAmount);
        miscBreakdown.WorkerMarginTotal = otherMiscEntries.Sum(e => e.ChargeAmount - e.PayAmount);
        report.Categories.Add(miscBreakdown);

        return report;
    }

    /// <summary>
    /// Boss own-work profit for hourly categories (Cleaning, Maintenance):
    /// HoursBilled × RateCharged
    /// </summary>
    private static decimal ComputeBossOwnHourly(
        List<WorkEntry> bossEntries, string category, List<Rate> rates, Guid? bossWorkerId)
    {
        var entries = bossEntries.Where(e => e.WorkCategoryValue == category).ToList();
        if (entries.Count == 0 || !bossWorkerId.HasValue) return 0;

        var rateCharged = ResolveRateCharged(rates, bossWorkerId.Value);

        return entries.Sum(e => (e.HoursBilled ?? 0) * rateCharged);
    }

    /// <summary>
    /// Worker margin for hourly categories (Cleaning, Maintenance):
    /// HoursBilled × (RateCharged - RatePaid)
    /// </summary>
    private static decimal ComputeWorkerMarginHourly(
        List<WorkEntry> otherEntries, string category, List<Rate> rates)
    {
        var entries = otherEntries.Where(e => e.WorkCategoryValue == category).ToList();
        if (entries.Count == 0) return 0;

        // Group by worker to resolve per-worker rates
        return entries.GroupBy(e => e.WorkerId).Sum(workerGroup =>
        {
            var workerId = workerGroup.Key;
            var (rateCharged, ratePaid) = ResolveRateChargedAndPaid(rates, workerId);
            var margin = rateCharged - ratePaid;
            return workerGroup.Sum(e => (e.HoursBilled ?? 0) * margin);
        });
    }

    /// <summary>
    /// Boss own-work profit for Laundry: NumberOfLoads × RateCharged
    /// </summary>
    private static decimal ComputeBossOwnLaundry(
        List<WorkEntry> bossEntries, List<Rate> rates, Guid? bossWorkerId)
    {
        var entries = bossEntries.Where(e => e.WorkCategoryValue == WorkCategory.Laundry.ToString()).ToList();
        if (entries.Count == 0 || !bossWorkerId.HasValue) return 0;

        var rateCharged = ResolveRateCharged(rates, bossWorkerId.Value);

        return entries.Sum(e => (e.NumberOfLoads ?? 0) * rateCharged);
    }

    /// <summary>
    /// Worker margin for Laundry: NumberOfLoads × (RateCharged - RatePaid)
    /// </summary>
    private static decimal ComputeWorkerMarginLaundry(
        List<WorkEntry> otherEntries, List<Rate> rates)
    {
        var entries = otherEntries.Where(e => e.WorkCategoryValue == WorkCategory.Laundry.ToString()).ToList();
        if (entries.Count == 0) return 0;

        return entries.GroupBy(e => e.WorkerId).Sum(workerGroup =>
        {
            var workerId = workerGroup.Key;
            var (rateCharged, ratePaid) = ResolveRateChargedAndPaid(rates, workerId);
            var margin = rateCharged - ratePaid;
            return workerGroup.Sum(e => (e.NumberOfLoads ?? 0) * margin);
        });
    }

    /// <summary>
    /// Boss own-work profit for Lawn: LawnHouseRate.RateCharged per entry
    /// </summary>
    private static decimal ComputeBossOwnLawn(
        List<WorkEntry> bossEntries, List<LawnHouseRate> lawnRates, Guid? bossWorkerId)
    {
        var entries = bossEntries.Where(e => e.WorkCategoryValue == WorkCategory.Lawn.ToString()).ToList();
        if (entries.Count == 0 || !bossWorkerId.HasValue) return 0;

        return entries.Sum(e =>
        {
            var rateCharged = ResolveLawnRateCharged(lawnRates, e.HouseId, bossWorkerId.Value);
            return rateCharged;
        });
    }

    /// <summary>
    /// Worker margin for Lawn: LawnHouseRate.RateCharged - LawnHouseRate.RatePaid per entry
    /// </summary>
    private static decimal ComputeWorkerMarginLawn(
        List<WorkEntry> otherEntries, List<LawnHouseRate> lawnRates)
    {
        var entries = otherEntries.Where(e => e.WorkCategoryValue == WorkCategory.Lawn.ToString()).ToList();
        if (entries.Count == 0) return 0;

        return entries.Sum(e =>
        {
            var (rateCharged, ratePaid) = ResolveLawnRateChargedAndPaid(lawnRates, e.HouseId, e.WorkerId);
            return rateCharged - ratePaid;
        });
    }

    /// <summary>
    /// Resolve RateCharged for a worker: worker-specific override first, then default (WorkerId == null).
    /// Returns 0 if no rate found.
    /// </summary>
    private static decimal ResolveRateCharged(List<Rate> rates, Guid workerId)
    {
        var workerRate = rates.FirstOrDefault(r => r.WorkerId == workerId);
        var defaultRate = rates.FirstOrDefault(r => r.WorkerId == null);
        return workerRate?.RateCharged ?? defaultRate?.RateCharged ?? 0;
    }

    /// <summary>
    /// Resolve both RateCharged and RatePaid for a worker.
    /// </summary>
    private static (decimal RateCharged, decimal RatePaid) ResolveRateChargedAndPaid(List<Rate> rates, Guid workerId)
    {
        var workerRate = rates.FirstOrDefault(r => r.WorkerId == workerId);
        var defaultRate = rates.FirstOrDefault(r => r.WorkerId == null);
        var rateCharged = workerRate?.RateCharged ?? defaultRate?.RateCharged ?? 0;
        var ratePaid = workerRate?.RatePaid ?? defaultRate?.RatePaid ?? 0;
        return (rateCharged, ratePaid);
    }

    /// <summary>
    /// Resolve LawnHouseRate RateCharged for a specific house and worker.
    /// </summary>
    private static decimal ResolveLawnRateCharged(List<LawnHouseRate> lawnRates, Guid houseId, Guid workerId)
    {
        var workerRate = lawnRates.FirstOrDefault(r => r.HouseId == houseId && r.WorkerId == workerId);
        var defaultRate = lawnRates.FirstOrDefault(r => r.HouseId == houseId && r.WorkerId == null);
        return workerRate?.RateCharged ?? defaultRate?.RateCharged ?? 0;
    }

    /// <summary>
    /// Resolve both LawnHouseRate RateCharged and RatePaid for a specific house and worker.
    /// </summary>
    private static (decimal RateCharged, decimal RatePaid) ResolveLawnRateChargedAndPaid(
        List<LawnHouseRate> lawnRates, Guid houseId, Guid workerId)
    {
        var workerRate = lawnRates.FirstOrDefault(r => r.HouseId == houseId && r.WorkerId == workerId);
        var defaultRate = lawnRates.FirstOrDefault(r => r.HouseId == houseId && r.WorkerId == null);
        var rateCharged = workerRate?.RateCharged ?? defaultRate?.RateCharged ?? 0;
        var ratePaid = workerRate?.RatePaid ?? defaultRate?.RatePaid ?? 0;
        return (rateCharged, ratePaid);
    }
}
