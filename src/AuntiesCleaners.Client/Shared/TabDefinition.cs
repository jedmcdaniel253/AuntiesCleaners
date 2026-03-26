using MudBlazor;

namespace AuntiesCleaners.Client.Shared;

public record TabDefinition(string Name, string Route, string Icon);

public static class AllTabs
{
    public static readonly TabDefinition Cleaning = new("Cleaning", "cleaning", Icons.Material.Filled.CleaningServices);
    public static readonly TabDefinition Laundry = new("Laundry", "laundry", Icons.Material.Filled.LocalLaundryService);
    public static readonly TabDefinition Maintenance = new("Maintenance", "maintenance", Icons.Material.Filled.Build);
    public static readonly TabDefinition Lawn = new("Lawn", "lawn", Icons.Material.Filled.Grass);
    public static readonly TabDefinition Receipts = new("Receipts", "receipts", Icons.Material.Filled.Receipt);
    public static readonly TabDefinition Calendar = new("Calendar", "calendar", Icons.Material.Filled.CalendarMonth);
    public static readonly TabDefinition MowList = new("Mow List", "mow-list", Icons.Material.Filled.Checklist);
    public static readonly TabDefinition MyEntries = new("My Entries", "my-entries", Icons.Material.Filled.ListAlt);
    public static readonly TabDefinition DailySummary = new("Daily Summary", "daily-summary", Icons.Material.Filled.Dashboard);
    public static readonly TabDefinition Workers = new("Workers", "workers", Icons.Material.Filled.Groups);
    public static readonly TabDefinition Houses = new("Houses", "houses", Icons.Material.Filled.Home);
    public static readonly TabDefinition Rates = new("Rates", "rates", Icons.Material.Filled.AttachMoney);
    public static readonly TabDefinition BillingReports = new("Billing", "billing-report", Icons.Material.Filled.Description);
    public static readonly TabDefinition WorkerPay = new("Worker Pay", "worker-pay", Icons.Material.Filled.Payments);
    public static readonly TabDefinition OwnerContact = new("Owner", "owner-contact", Icons.Material.Filled.ContactPhone);
    public static readonly TabDefinition Users = new("Users", "users", Icons.Material.Filled.ManageAccounts);
    public static readonly TabDefinition TabConfig = new("Tab Config", "tab-config", Icons.Material.Filled.Tab);

    public static readonly Dictionary<string, TabDefinition> ByName = new()
    {
        ["Cleaning"] = Cleaning, ["Laundry"] = Laundry, ["Maintenance"] = Maintenance,
        ["Lawn"] = Lawn, ["Receipts"] = Receipts, ["Calendar"] = Calendar,
        ["Mow List"] = MowList, ["My Entries"] = MyEntries, ["Daily Summary"] = DailySummary,
        ["Workers"] = Workers, ["Houses"] = Houses, ["Rates"] = Rates,
        ["Billing"] = BillingReports, ["Worker Pay"] = WorkerPay, ["Owner"] = OwnerContact,
        ["Users"] = Users, ["Tab Config"] = TabConfig
    };
}
