using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public static class RoleFeatureMap
{
    public static readonly HashSet<string> WorkerFeatures = new()
    {
        "Cleaning", "Laundry", "Maintenance", "Lawn",
        "Receipts", "Calendar", "MowList", "MyEntries"
    };

    public static readonly HashSet<string> ManagerFeatures = new(WorkerFeatures)
    {
        "DailySummary", "CalendarCRUD", "MowListToggle"
    };

    public static readonly HashSet<string> BossFeatures = new(ManagerFeatures)
    {
        "Rates", "MiscEntriesCRUD", "BillingReports", "WorkerPay", "OwnerContact",
        "Workers", "Houses", "Users", "TabConfig"
    };

    public static readonly HashSet<string> AdminFeatures = new(BossFeatures);

    public static HashSet<string> GetFeatures(Role role) => role switch
    {
        Role.Worker => WorkerFeatures,
        Role.Manager => ManagerFeatures,
        Role.Boss => BossFeatures,
        Role.Admin => AdminFeatures,
        _ => new HashSet<string>()
    };

    public static HashSet<string> GetPrimaryNavFeatures(Role role) => role switch
    {
        Role.Boss => new HashSet<string>(BossFeatures.Except(new[] { "Workers", "Houses", "Users", "TabConfig" })),
        _ => GetFeatures(role)
    };

    public static HashSet<string> GetOverflowFeatures(Role role) => role switch
    {
        Role.Boss => new HashSet<string>(new[] { "Workers", "Houses", "Users", "TabConfig" }),
        _ => new HashSet<string>()
    };
}
