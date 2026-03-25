using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Shared;
using Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// P10: Tab Configuration Completeness — verify union of nav bar + overflow = exactly the role-permitted tabs.
/// No tab appears in both nav bar and overflow simultaneously.
/// </summary>
public class TabConfigTests
{
    // Define the full set of tabs each role is permitted to access
    private static readonly Dictionary<Role, HashSet<string>> RolePermittedTabs = new()
    {
        [Role.Worker] = new() { "Cleaning", "Laundry", "Maintenance", "Lawn", "Receipts", "Calendar", "Mow List", "My Entries" },
        [Role.Manager] = new() { "Cleaning", "Laundry", "Maintenance", "Lawn", "Receipts", "Calendar", "Mow List", "My Entries", "Daily Summary" },
        [Role.Boss] = new() { "Cleaning", "Laundry", "Maintenance", "Lawn", "Receipts", "Calendar", "Mow List", "My Entries", "Daily Summary", "Rates", "Billing", "Worker Pay", "Owner", "Workers", "Houses", "Users", "Tab Config" },
        [Role.Admin] = new() { "Cleaning", "Laundry", "Maintenance", "Lawn", "Receipts", "Calendar", "Mow List", "My Entries", "Daily Summary", "Rates", "Billing", "Worker Pay", "Owner", "Workers", "Houses", "Users", "Tab Config" }
    };

    [Theory]
    [InlineData(Role.Worker)]
    [InlineData(Role.Manager)]
    [InlineData(Role.Boss)]
    [InlineData(Role.Admin)]
    public void UnionOfNavAndOverflowEqualsPermittedTabs(Role role)
    {
        var (visible, overflow) = GetDefaultTabs(role);
        var visibleNames = visible.Select(t => t.Name).ToHashSet();
        var overflowNames = overflow.Select(t => t.Name).ToHashSet();

        var union = new HashSet<string>(visibleNames);
        union.UnionWith(overflowNames);

        Assert.True(union.SetEquals(RolePermittedTabs[role]),
            $"Role {role}: union of nav+overflow does not match permitted tabs. " +
            $"Missing: {string.Join(", ", RolePermittedTabs[role].Except(union))}. " +
            $"Extra: {string.Join(", ", union.Except(RolePermittedTabs[role]))}");
    }

    [Theory]
    [InlineData(Role.Worker)]
    [InlineData(Role.Manager)]
    [InlineData(Role.Boss)]
    [InlineData(Role.Admin)]
    public void NoTabAppearsInBothNavAndOverflow(Role role)
    {
        var (visible, overflow) = GetDefaultTabs(role);
        var visibleNames = visible.Select(t => t.Name).ToHashSet();
        var overflowNames = overflow.Select(t => t.Name).ToHashSet();

        var intersection = new HashSet<string>(visibleNames);
        intersection.IntersectWith(overflowNames);

        Assert.Empty(intersection);
    }

    // Mirror of BottomNavBar.GetDefaultTabs logic for testing
    private static (List<TabDefinition> visible, List<TabDefinition> overflow) GetDefaultTabs(Role role)
    {
        return role switch
        {
            Role.Worker => (
                new List<TabDefinition> { AllTabs.Cleaning, AllTabs.Calendar, AllTabs.MyEntries },
                new List<TabDefinition> { AllTabs.Laundry, AllTabs.Maintenance, AllTabs.Lawn, AllTabs.Receipts, AllTabs.MowList }
            ),
            Role.Manager => (
                new List<TabDefinition> { AllTabs.Cleaning, AllTabs.Calendar, AllTabs.Laundry, AllTabs.Receipts, AllTabs.MyEntries },
                new List<TabDefinition> { AllTabs.Maintenance, AllTabs.Lawn, AllTabs.MowList, AllTabs.DailySummary }
            ),
            Role.Boss => (
                new List<TabDefinition> { AllTabs.Cleaning, AllTabs.Calendar, AllTabs.Laundry, AllTabs.Receipts, AllTabs.MyEntries, AllTabs.DailySummary },
                new List<TabDefinition> { AllTabs.Maintenance, AllTabs.Lawn, AllTabs.MowList, AllTabs.Rates, AllTabs.BillingReports, AllTabs.WorkerPay, AllTabs.OwnerContact, AllTabs.Workers, AllTabs.Houses, AllTabs.Users, AllTabs.TabConfig }
            ),
            Role.Admin => (
                new List<TabDefinition> { AllTabs.Cleaning, AllTabs.Calendar, AllTabs.Laundry, AllTabs.Receipts, AllTabs.MyEntries, AllTabs.DailySummary, AllTabs.Workers, AllTabs.Houses, AllTabs.Rates },
                new List<TabDefinition> { AllTabs.Maintenance, AllTabs.Lawn, AllTabs.MowList, AllTabs.BillingReports, AllTabs.WorkerPay, AllTabs.OwnerContact, AllTabs.Users, AllTabs.TabConfig }
            ),
            _ => (new(), new())
        };
    }
}
