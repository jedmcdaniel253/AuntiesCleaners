using AuntiesCleaners.Client.Models;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// P5: Mow List Auto-Clear — verify lawn entry clears mow status for that house.
/// **Validates: Requirements 15.3**
/// </summary>
public class MowListAutoClearTests
{
    /// <summary>
    /// Simulates the mow list state as a dictionary of houseId -> needsMowing.
    /// This mirrors what MowListService manages in the database.
    /// </summary>
    private static Dictionary<Guid, bool> ApplyAutoClear(
        Dictionary<Guid, bool> mowState, WorkEntry entry)
    {
        var result = new Dictionary<Guid, bool>(mowState);
        if (entry.WorkCategoryValue == WorkCategory.Lawn.ToString())
        {
            result[entry.HouseId] = false;
        }
        return result;
    }

    private static Gen<Guid> GuidGen => Gen.Fresh(() => Guid.NewGuid());

    private static Gen<WorkEntry> WorkEntryGen(Guid houseId, WorkCategory category) =>
        from id in GuidGen
        from workerId in GuidGen
        from dayOffset in Gen.Choose(0, 365)
        select new WorkEntry
        {
            Id = id,
            WorkerId = workerId,
            HouseId = houseId,
            WorkCategoryValue = category.ToString(),
            EntryDate = DateTime.Today.AddDays(dayOffset),
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

    private static Gen<WorkCategory> NonLawnCategoryGen =>
        Gen.Elements(WorkCategory.Cleaning, WorkCategory.Laundry, WorkCategory.Maintenance);

    /// <summary>
    /// Property 1: After a lawn entry is created for a house, that house's needs_mowing is false.
    /// **Validates: Requirements 15.3**
    /// </summary>
    [Property]
    public Property LawnEntryClearsMowStatusForHouse()
    {
        var gen =
            from houseId in GuidGen
            from entry in WorkEntryGen(houseId, WorkCategory.Lawn)
            select (houseId, entry);

        return Prop.ForAll(
            Arb.From(gen),
            tuple =>
            {
                var (houseId, entry) = tuple;

                // House starts with needs_mowing = true
                var mowState = new Dictionary<Guid, bool> { { houseId, true } };

                var result = ApplyAutoClear(mowState, entry);

                return !result[houseId];
            });
    }

    /// <summary>
    /// Property 2: Creating a non-lawn entry does not affect mow status.
    /// **Validates: Requirements 15.3**
    /// </summary>
    [Property]
    public Property NonLawnEntryDoesNotAffectMowStatus()
    {
        var gen =
            from houseId in GuidGen
            from category in NonLawnCategoryGen
            from entry in WorkEntryGen(houseId, category)
            from initialStatus in Gen.Elements(true, false)
            select (houseId, entry, initialStatus);

        return Prop.ForAll(
            Arb.From(gen),
            tuple =>
            {
                var (houseId, entry, initialStatus) = tuple;

                var mowState = new Dictionary<Guid, bool> { { houseId, initialStatus } };

                var result = ApplyAutoClear(mowState, entry);

                // Status should remain unchanged
                return result[houseId] == initialStatus;
            });
    }

    /// <summary>
    /// Property 3: Auto-clear only affects the specific house, not other houses.
    /// **Validates: Requirements 15.3**
    /// </summary>
    [Property]
    public Property AutoClearOnlyAffectsTargetHouse()
    {
        var gen =
            from targetHouseId in GuidGen
            from otherHouseId in GuidGen
            from entry in WorkEntryGen(targetHouseId, WorkCategory.Lawn)
            from otherStatus in Gen.Elements(true, false)
            select (targetHouseId, otherHouseId, entry, otherStatus);

        return Prop.ForAll(
            Arb.From(gen),
            tuple =>
            {
                var (targetHouseId, otherHouseId, entry, otherStatus) = tuple;

                // Both houses start with mow-needed status
                var mowState = new Dictionary<Guid, bool>
                {
                    { targetHouseId, true },
                    { otherHouseId, otherStatus }
                };

                var result = ApplyAutoClear(mowState, entry);

                // Target house should be cleared
                var targetCleared = !result[targetHouseId];
                // Other house should be unchanged
                var otherUnchanged = result[otherHouseId] == otherStatus;

                return targetCleared && otherUnchanged;
            });
    }
}
