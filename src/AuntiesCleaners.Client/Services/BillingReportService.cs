using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public class BillingReportService : IBillingReportService
{
    private readonly IWorkEntryService _workEntryService;
    private readonly IMiscEntryService _miscEntryService;
    private readonly IReceiptService _receiptService;
    private readonly IRateService _rateService;
    private readonly IHouseService _houseService;

    public BillingReportService(
        IWorkEntryService workEntryService,
        IMiscEntryService miscEntryService,
        IReceiptService receiptService,
        IRateService rateService,
        IHouseService houseService)
    {
        _workEntryService = workEntryService;
        _miscEntryService = miscEntryService;
        _receiptService = receiptService;
        _rateService = rateService;
        _houseService = houseService;
    }

    public async Task<BillingReport> GenerateReportAsync(DateTime from, DateTime to)
    {
        var workEntries = await _workEntryService.GetByDateRangeAsync(from, to);
        var miscEntries = await _miscEntryService.GetByDateRangeAsync(from, to);
        var receipts = await _receiptService.GetByDateRangeAsync(from, to);
        var houses = await _houseService.GetAllAsync();
        var houseMap = houses.ToDictionary(h => h.Id, h => h.Name);

        var cleaningRates = await _rateService.GetByCategoryAsync("Cleaning");
        var laundryRates = await _rateService.GetByCategoryAsync("Laundry");
        var maintenanceRates = await _rateService.GetByCategoryAsync("Maintenance");
        var lawnRates = await _rateService.GetAllLawnRatesAsync();

        var report = new BillingReport { DateFrom = from, DateTo = to };

        var cleaningSection = BuildWorkSection("Cleaning", WorkCategory.Cleaning, workEntries, cleaningRates, houseMap);
        if (cleaningSection.LineItems.Count > 0) report.Sections.Add(cleaningSection);

        var laundrySection = BuildLaundrySection(workEntries, laundryRates, houseMap);
        if (laundrySection.LineItems.Count > 0) report.Sections.Add(laundrySection);

        var mowingSection = BuildMowingSection(workEntries, lawnRates, houseMap);
        if (mowingSection.LineItems.Count > 0) report.Sections.Add(mowingSection);

        var maintenanceSection = BuildWorkSection("Maintenance", WorkCategory.Maintenance, workEntries, maintenanceRates, houseMap);
        if (maintenanceSection.LineItems.Count > 0) report.Sections.Add(maintenanceSection);

        var miscSection = BuildMiscSection(miscEntries, houseMap);
        if (miscSection.LineItems.Count > 0) report.Sections.Add(miscSection);

        var receiptSection = BuildReceiptSection(receipts);
        if (receiptSection.LineItems.Count > 0) report.Sections.Add(receiptSection);

        return report;
    }

    private static BillingSection BuildWorkSection(
        string sectionName, WorkCategory category,
        List<WorkEntry> allEntries, List<Rate> rates,
        Dictionary<Guid, string> houseMap)
    {
        var entries = allEntries.Where(e => e.WorkCategoryValue == category.ToString()).ToList();
        var defaultRate = rates.FirstOrDefault(r => r.WorkerId == null);
        var section = new BillingSection { Name = sectionName };

        foreach (var entry in entries)
        {
            var hours = entry.HoursBilled ?? 0;
            var rateCharged = defaultRate?.RateCharged ?? 0;
            var houseName = houseMap.GetValueOrDefault(entry.HouseId, "Unknown");
            var amount = hours * rateCharged;

            section.LineItems.Add(new BillingLineItem
            {
                Description = $"{houseName} — {entry.EntryDate:MM/dd/yyyy} — {hours:F1} hrs @ ${rateCharged:F2}/hr",
                Amount = amount
            });
        }

        return section;
    }

    private static BillingSection BuildLaundrySection(
        List<WorkEntry> allEntries, List<Rate> rates,
        Dictionary<Guid, string> houseMap)
    {
        var entries = allEntries.Where(e => e.WorkCategoryValue == WorkCategory.Laundry.ToString()).ToList();
        var defaultRate = rates.FirstOrDefault(r => r.WorkerId == null);
        var section = new BillingSection { Name = "Laundry" };

        foreach (var entry in entries)
        {
            var loads = entry.NumberOfLoads ?? 0;
            var rateCharged = defaultRate?.RateCharged ?? 0;
            var houseName = houseMap.GetValueOrDefault(entry.HouseId, "Unknown");
            var amount = loads * rateCharged;

            section.LineItems.Add(new BillingLineItem
            {
                Description = $"{houseName} — {entry.EntryDate:MM/dd/yyyy} — {loads} loads @ ${rateCharged:F2}/load",
                Amount = amount
            });
        }

        return section;
    }

    private static BillingSection BuildMowingSection(
        List<WorkEntry> allEntries, List<LawnHouseRate> lawnRates,
        Dictionary<Guid, string> houseMap)
    {
        var entries = allEntries.Where(e => e.WorkCategoryValue == WorkCategory.Lawn.ToString()).ToList();
        var section = new BillingSection { Name = "Mowing" };

        foreach (var entry in entries)
        {
            var defaultLawnRate = lawnRates.FirstOrDefault(r => r.HouseId == entry.HouseId && r.WorkerId == null);
            var rateCharged = defaultLawnRate?.RateCharged ?? 0;
            var houseName = houseMap.GetValueOrDefault(entry.HouseId, "Unknown");

            section.LineItems.Add(new BillingLineItem
            {
                Description = $"{houseName} — {entry.EntryDate:MM/dd/yyyy} — Flat rate ${rateCharged:F2}",
                Amount = rateCharged
            });
        }

        return section;
    }

    private static BillingSection BuildMiscSection(
        List<MiscellaneousEntry> entries,
        Dictionary<Guid, string> houseMap)
    {
        var section = new BillingSection { Name = "Miscellaneous" };

        foreach (var entry in entries)
        {
            var housePart = entry.HouseId.HasValue
                ? $" — {houseMap.GetValueOrDefault(entry.HouseId.Value, "Unknown")}"
                : string.Empty;

            section.LineItems.Add(new BillingLineItem
            {
                Description = $"{entry.Description}{housePart} — {entry.EntryDate:MM/dd/yyyy}",
                Amount = entry.ChargeAmount
            });
        }

        return section;
    }

    public static BillingSection BuildReceiptSection(List<Receipt> receipts)
    {
        var section = new BillingSection { Name = "Receipts" };

        for (int i = 0; i < receipts.Count; i++)
        {
            var receipt = receipts[i];
            var refNum = $"R-{(i + 1):D3}";
            var amount = receipt.IsReimbursable ? receipt.Amount : 0.00m;

            section.LineItems.Add(new BillingLineItem
            {
                Description = $"{refNum} — {receipt.BusinessName} — {receipt.ReceiptDate:MM/dd/yyyy}",
                Amount = amount
            });
        }

        return section;
    }
}
