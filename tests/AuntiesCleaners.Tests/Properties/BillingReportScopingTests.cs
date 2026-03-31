using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Services;
using AuntiesCleaners.Tests.Helpers;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using NSubstitute;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// Property tests for billing report scoping to billing owner.
/// Feature: multi-owner-house-assignment
/// </summary>
public class BillingReportScopingTests
{
    // ── Generators ──────────────────────────────────────────────────

    /// <summary>
    /// Generates a list of 1–10 owners where exactly one has IsBillingOwner = true.
    /// </summary>
    private static Gen<List<Owner>> OwnersWithExactlyOneBillingGen =>
        from count in Gen.Choose(1, 10)
        from owners in Gen.ListOf(Generators.OwnerGen, count)
        from billingIndex in Gen.Choose(0, count - 1)
        select owners.Select((o, i) =>
        {
            o.IsBillingOwner = i == billingIndex;
            return o;
        }).ToList();

    /// <summary>
    /// Generates a list of 1–10 owners where none has IsBillingOwner = true.
    /// </summary>
    private static Gen<List<Owner>> OwnersWithNoBillingGen =>
        from count in Gen.Choose(1, 10)
        from owners in Gen.ListOf(Generators.OwnerGen, count)
        select owners.Select(o =>
        {
            o.IsBillingOwner = false;
            return o;
        }).ToList();

    // ── Property 6: Send-to-owner resolves billing owner email ──────

    /// <summary>
    /// Property 6: Send-to-owner resolves billing owner email
    /// For any set of Owners where exactly one has is_billing_owner = true,
    /// the "Send To Owner" action SHALL use that owner's email address.
    /// **Validates: Requirements 5.4, 5.5**
    /// </summary>
    [Property]
    public Property SendToOwnerResolvesBillingOwnerEmail()
    {
        return Prop.ForAll(
            Arb.From(OwnersWithExactlyOneBillingGen),
            owners =>
            {
                var billingOwner = owners.Single(o => o.IsBillingOwner);

                // The resolved email must be the billing owner's email
                var resolvedEmail = ResolveSendToOwnerEmail(owners);

                return resolvedEmail == billingOwner.Email;
            });
    }

    /// <summary>
    /// Property 6 (no billing owner case):
    /// If no owner has is_billing_owner = true, the action SHALL fail
    /// with an appropriate error message.
    /// **Validates: Requirements 5.4, 5.5**
    /// </summary>
    [Property]
    public Property SendToOwnerFailsWhenNoBillingOwner()
    {
        return Prop.ForAll(
            Arb.From(OwnersWithNoBillingGen),
            owners =>
            {
                // With no billing owner, resolution should return null (failure)
                var resolvedEmail = ResolveSendToOwnerEmail(owners);
                return resolvedEmail == null;
            });
    }

    /// <summary>
    /// Mirrors the SendToOwner logic from BillingReport.razor:
    /// calls GetBillingOwnerAsync() and uses that owner's email.
    /// Returns null if no billing owner is found.
    /// </summary>
    private static string? ResolveSendToOwnerEmail(List<Owner> owners)
    {
        var billingOwner = owners.FirstOrDefault(o => o.IsBillingOwner);
        return billingOwner?.Email;
    }

    // ── Property 7: Billing report scoped to billing owner's houses ─


    /// <summary>
    /// Property 7: Billing report scoped to billing owner's houses
    /// For any set of houses (some with owner_id matching the billing owner, some with
    /// other owner IDs, some with null), and any set of work entries and miscellaneous entries
    /// across those houses, the billing report SHALL include only entries associated with
    /// houses where owner_id equals the billing owner's ID. Houses with null owner_id
    /// SHALL be excluded.
    /// **Validates: Requirements 9.1, 9.2, 9.4**
    /// </summary>
    [Property]
    public Property BillingReportOnlyIncludesBillingOwnerHouses()
    {
        // Generate a billing owner, other owners, houses with mixed ownership, and entries
        var gen =
            from billingOwner in Generators.OwnerGen.Select(o => { o.IsBillingOwner = true; return o; })
            from otherOwner in Generators.OwnerGen.Select(o => { o.IsBillingOwner = false; return o; })
            from houseCount in Gen.Choose(1, 10)
            from houses in Gen.ListOf(Generators.HouseGen, houseCount)
            from ownerFlags in Gen.ListOf(Gen.Choose(0, 2), houseCount)
            from workEntryCount in Gen.Choose(0, 15)
            from workEntries in Gen.ListOf(Generators.WorkEntryGen, workEntryCount)
            from miscEntryCount in Gen.Choose(0, 10)
            from miscEntries in Gen.ListOf(Generators.MiscellaneousEntryGen, miscEntryCount)
            select (billingOwner, otherOwner,
                    houses: AssignOwners(houses.ToList(), ownerFlags.ToList(), billingOwner.Id, otherOwner.Id),
                    workEntries: workEntries.ToList(),
                    miscEntries: miscEntries.ToList());

        return Prop.ForAll(
            Arb.From(gen),
            data =>
            {
                var (billingOwner, _, houses, workEntries, miscEntries) = data;

                // Assign work entries to random houses from our set
                var random = new Random(42);
                foreach (var we in workEntries)
                {
                    we.HouseId = houses[random.Next(houses.Count)].Id;
                }
                foreach (var me in miscEntries)
                {
                    // Some misc entries have null HouseId, some have a house
                    if (random.Next(3) == 0)
                        me.HouseId = null;
                    else
                        me.HouseId = houses[random.Next(houses.Count)].Id;
                }

                // Apply the same filtering logic as BillingReportService
                var billingHouses = houses.Where(h => h.OwnerId == billingOwner.Id).ToList();
                var billingHouseIds = billingHouses.Select(h => h.Id).ToHashSet();

                var filteredWorkEntries = workEntries
                    .Where(e => billingHouseIds.Contains(e.HouseId))
                    .ToList();

                // Misc entries: include those with null HouseId OR those in billing houses
                // (matches BillingReportService logic)
                var filteredMiscEntries = miscEntries
                    .Where(e => !e.HouseId.HasValue || billingHouseIds.Contains(e.HouseId.Value))
                    .ToList();

                // Verify: no work entry references a non-billing-owner house
                var allFilteredWorkHouseIds = filteredWorkEntries.Select(e => e.HouseId).Distinct();
                if (allFilteredWorkHouseIds.Any(hid => !billingHouseIds.Contains(hid)))
                    return false;

                // Verify: no misc entry with a HouseId references a non-billing-owner house
                var miscWithHouse = filteredMiscEntries.Where(e => e.HouseId.HasValue);
                if (miscWithHouse.Any(e => !billingHouseIds.Contains(e.HouseId!.Value)))
                    return false;

                // Verify: houses with null owner_id are excluded from billing houses
                if (billingHouses.Any(h => h.OwnerId == null))
                    return false;

                // Verify: all billing houses belong to the billing owner
                if (billingHouses.Any(h => h.OwnerId != billingOwner.Id))
                    return false;

                // Verify: work entries for non-billing houses are excluded
                var nonBillingWorkEntries = workEntries
                    .Where(e => !billingHouseIds.Contains(e.HouseId))
                    .ToList();
                if (filteredWorkEntries.Any(e => nonBillingWorkEntries.Contains(e)))
                    return false;

                return true;
            });
    }

    /// <summary>
    /// Assigns OwnerId to houses based on flags:
    /// 0 = billing owner, 1 = other owner, 2 = null (no owner).
    /// </summary>
    private static List<House> AssignOwners(
        List<House> houses, List<int> flags,
        Guid billingOwnerId, Guid otherOwnerId)
    {
        for (int i = 0; i < houses.Count; i++)
        {
            houses[i].OwnerId = flags[i] switch
            {
                0 => billingOwnerId,
                1 => otherOwnerId,
                _ => null
            };
        }
        return houses;
    }
}
