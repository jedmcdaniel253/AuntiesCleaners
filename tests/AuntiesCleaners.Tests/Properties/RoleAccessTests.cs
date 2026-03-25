using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Services;
using Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// P4: Role Access Control — verify each role maps to exactly the correct set of permitted features.
/// Role hierarchy: Worker ⊂ Manager ⊂ Boss = Admin
/// </summary>
public class RoleAccessTests
{
    [Theory]
    [InlineData(Role.Worker)]
    [InlineData(Role.Manager)]
    [InlineData(Role.Boss)]
    [InlineData(Role.Admin)]
    public void EachRoleGetsExactlyItsFeatures(Role role)
    {
        var expected = role switch
        {
            Role.Worker => RoleFeatureMap.WorkerFeatures,
            Role.Manager => RoleFeatureMap.ManagerFeatures,
            Role.Boss => RoleFeatureMap.BossFeatures,
            Role.Admin => RoleFeatureMap.AdminFeatures,
            _ => new HashSet<string>()
        };
        Assert.True(RoleFeatureMap.GetFeatures(role).SetEquals(expected));
    }

    [Fact]
    public void WorkerFeaturesAreSubsetOfManager()
    {
        Assert.True(RoleFeatureMap.WorkerFeatures.IsSubsetOf(RoleFeatureMap.ManagerFeatures));
    }

    [Fact]
    public void ManagerFeaturesAreSubsetOfBoss()
    {
        Assert.True(RoleFeatureMap.ManagerFeatures.IsSubsetOf(RoleFeatureMap.BossFeatures));
    }

    [Fact]
    public void BossFeaturesEqualAdmin()
    {
        Assert.True(RoleFeatureMap.BossFeatures.SetEquals(RoleFeatureMap.AdminFeatures));
    }

    [Fact]
    public void WorkerCannotAccessDailySummary()
    {
        Assert.DoesNotContain("DailySummary", RoleFeatureMap.WorkerFeatures);
    }

    [Fact]
    public void WorkerCannotAccessRates()
    {
        Assert.DoesNotContain("Rates", RoleFeatureMap.WorkerFeatures);
        Assert.DoesNotContain("Rates", RoleFeatureMap.ManagerFeatures);
    }

    [Fact]
    public void ManagerCannotAccessBillingOrPay()
    {
        Assert.DoesNotContain("BillingReports", RoleFeatureMap.ManagerFeatures);
        Assert.DoesNotContain("WorkerPay", RoleFeatureMap.ManagerFeatures);
    }

    [Fact]
    public void BossConfigFeaturesInOverflow()
    {
        var overflow = RoleFeatureMap.GetOverflowFeatures(Role.Boss);
        Assert.Contains("Workers", overflow);
        Assert.Contains("Houses", overflow);
        Assert.Contains("Users", overflow);
        Assert.Contains("TabConfig", overflow);
    }

    [Fact]
    public void AdminHasNoOverflowRestrictions()
    {
        var overflow = RoleFeatureMap.GetOverflowFeatures(Role.Admin);
        Assert.Empty(overflow);
    }

    [Fact]
    public void ManagerCanAccessCalendarCRUD()
    {
        Assert.Contains("CalendarCRUD", RoleFeatureMap.ManagerFeatures);
        Assert.DoesNotContain("CalendarCRUD", RoleFeatureMap.WorkerFeatures);
    }

    [Fact]
    public void ManagerCanToggleMowList()
    {
        Assert.Contains("MowListToggle", RoleFeatureMap.ManagerFeatures);
        Assert.DoesNotContain("MowListToggle", RoleFeatureMap.WorkerFeatures);
    }
}
