using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public class WorkerPayService : IWorkerPayService
{
    private readonly IWorkEntryService _workEntryService;
    private readonly IMiscEntryService _miscEntryService;
    private readonly IRateService _rateService;
    private readonly IWorkerService _workerService;
    private readonly IHouseService _houseService;

    public WorkerPayService(
        IWorkEntryService workEntryService,
        IMiscEntryService miscEntryService,
        IRateService rateService,
        IWorkerService workerService,
        IHouseService houseService)
    {
        _workEntryService = workEntryService;
        _miscEntryService = miscEntryService;
        _rateService = rateService;
        _workerService = workerService;
        _houseService = houseService;
    }

    public async Task<WorkerPayReport> GeneratePayReportAsync(DateTime from, DateTime to)
    {
        var workEntries = await _workEntryService.GetByDateRangeAsync(from, to);
        var miscEntries = await _miscEntryService.GetByDateRangeAsync(from, to);
        var workers = await _workerService.GetAllAsync();
        var houses = await _houseService.GetAllAsync();
        var houseMap = houses.ToDictionary(h => h.Id, h => h.Name);

        var cleaningRates = await _rateService.GetByCategoryAsync("Cleaning");
        var laundryRates = await _rateService.GetByCategoryAsync("Laundry");
        var maintenanceRates = await _rateService.GetByCategoryAsync("Maintenance");
        var lawnRates = await _rateService.GetAllLawnRatesAsync();

        // Find all worker IDs that have entries in the date range
        var workerIdsWithWork = workEntries.Select(e => e.WorkerId).ToHashSet();
        var workerIdsWithMisc = miscEntries.Select(e => e.WorkerId).ToHashSet();
        var activeWorkerIds = workerIdsWithWork.Union(workerIdsWithMisc).ToHashSet();

        var report = new WorkerPayReport { DateFrom = from, DateTo = to };

        foreach (var worker in workers.Where(w => activeWorkerIds.Contains(w.Id)))
        {
            var payment = new WorkerPayment { WorkerName = worker.Name };

            // Cleaning pay
            var workerCleaningEntries = workEntries
                .Where(e => e.WorkerId == worker.Id && e.WorkCategoryValue == WorkCategory.Cleaning.ToString())
                .ToList();
            var cleaningWorkerRate = cleaningRates.FirstOrDefault(r => r.WorkerId == worker.Id);
            var cleaningDefaultRate = cleaningRates.FirstOrDefault(r => r.WorkerId == null);
            var cleaningRatePaid = cleaningWorkerRate?.RatePaid ?? cleaningDefaultRate?.RatePaid ?? 0;

            foreach (var entry in workerCleaningEntries)
            {
                var hours = entry.HoursBilled ?? 0;
                var amount = hours * cleaningRatePaid;
                var houseName = houseMap.GetValueOrDefault(entry.HouseId, "Unknown");
                payment.CleaningItems.Add(new WorkerPayLineItem
                {
                    Description = $"{houseName} — {entry.EntryDate:MM/dd/yyyy} — {hours:F1} hrs × ${cleaningRatePaid:F2}/hr",
                    Amount = amount
                });
            }
            payment.CleaningPay = payment.CleaningItems.Sum(i => i.Amount);

            // Laundry pay
            var workerLaundryEntries = workEntries
                .Where(e => e.WorkerId == worker.Id && e.WorkCategoryValue == WorkCategory.Laundry.ToString())
                .ToList();
            var laundryWorkerRate = laundryRates.FirstOrDefault(r => r.WorkerId == worker.Id);
            var laundryDefaultRate = laundryRates.FirstOrDefault(r => r.WorkerId == null);
            var laundryRatePaid = laundryWorkerRate?.RatePaid ?? laundryDefaultRate?.RatePaid ?? 0;

            foreach (var entry in workerLaundryEntries)
            {
                var loads = entry.NumberOfLoads ?? 0;
                var amount = loads * laundryRatePaid;
                var houseName = houseMap.GetValueOrDefault(entry.HouseId, "Unknown");
                payment.LaundryItems.Add(new WorkerPayLineItem
                {
                    Description = $"{houseName} — {entry.EntryDate:MM/dd/yyyy} — {loads} loads × ${laundryRatePaid:F2}/load = ${amount:F2}",
                    Amount = amount
                });
            }
            payment.LaundryPay = payment.LaundryItems.Sum(i => i.Amount);

            // Maintenance pay
            var workerMaintenanceEntries = workEntries
                .Where(e => e.WorkerId == worker.Id && e.WorkCategoryValue == WorkCategory.Maintenance.ToString())
                .ToList();
            var maintenanceWorkerRate = maintenanceRates.FirstOrDefault(r => r.WorkerId == worker.Id);
            var maintenanceDefaultRate = maintenanceRates.FirstOrDefault(r => r.WorkerId == null);
            var maintenanceRatePaid = maintenanceWorkerRate?.RatePaid ?? maintenanceDefaultRate?.RatePaid ?? 0;

            foreach (var entry in workerMaintenanceEntries)
            {
                var hours = entry.HoursBilled ?? 0;
                var amount = hours * maintenanceRatePaid;
                var houseName = houseMap.GetValueOrDefault(entry.HouseId, "Unknown");
                payment.MaintenanceItems.Add(new WorkerPayLineItem
                {
                    Description = $"{houseName} — {entry.EntryDate:MM/dd/yyyy} — {hours:F1} hrs × ${maintenanceRatePaid:F2}/hr",
                    Amount = amount
                });
            }
            payment.MaintenancePay = payment.MaintenanceItems.Sum(i => i.Amount);

            // Lawn pay
            var workerLawnEntries = workEntries
                .Where(e => e.WorkerId == worker.Id && e.WorkCategoryValue == WorkCategory.Lawn.ToString())
                .ToList();

            // Group lawn entries by house for count × rate display
            var lawnByHouse = workerLawnEntries.GroupBy(e => e.HouseId);
            foreach (var houseGroup in lawnByHouse)
            {
                var houseId = houseGroup.Key;
                var count = houseGroup.Count();
                var houseName = houseMap.GetValueOrDefault(houseId, "Unknown");
                var lawnWorkerRate = lawnRates.FirstOrDefault(r => r.HouseId == houseId && r.WorkerId == worker.Id);
                var lawnDefaultRate = lawnRates.FirstOrDefault(r => r.HouseId == houseId && r.WorkerId == null);
                var lawnRatePaid = lawnWorkerRate?.RatePaid ?? lawnDefaultRate?.RatePaid ?? 0;
                var amount = count * lawnRatePaid;

                payment.LawnItems.Add(new WorkerPayLineItem
                {
                    Description = $"{houseName} — {count} mow(s) × ${lawnRatePaid:F2}/mow",
                    Amount = amount
                });
            }
            payment.LawnPay = payment.LawnItems.Sum(i => i.Amount);

            // Misc pay
            var workerMiscEntries = miscEntries.Where(e => e.WorkerId == worker.Id).ToList();
            foreach (var entry in workerMiscEntries)
            {
                payment.MiscItems.Add(new WorkerPayLineItem
                {
                    Description = $"{entry.Description} — {entry.EntryDate:MM/dd/yyyy}",
                    Amount = entry.PayAmount
                });
            }
            payment.MiscPay = payment.MiscItems.Sum(i => i.Amount);

            report.WorkerPayments.Add(payment);
        }

        return report;
    }
}
