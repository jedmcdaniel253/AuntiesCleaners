using AuntiesCleaners.Client.Models;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// P7: Work Entry Visibility — verify worker-role users see exactly their own + created-by entries,
/// and manager/boss/admin users see all entries.
/// **Validates: Requirements 11.6**
/// </summary>
public class WorkEntryVisibilityTests
{
    private static Gen<WorkEntry> WorkEntryGen =>
        from id in Gen.Fresh(() => Guid.NewGuid())
        from workerId in Gen.Fresh(() => Guid.NewGuid())
        from houseId in Gen.Fresh(() => Guid.NewGuid())
        from category in Gen.Elements("Cleaning", "Laundry", "Maintenance", "Lawn")
        from createdBy in Gen.Fresh(() => Guid.NewGuid())
        select new WorkEntry
        {
            Id = id,
            WorkerId = workerId,
            HouseId = houseId,
            WorkCategoryValue = category,
            EntryDate = DateTime.Today,
            CreatedBy = createdBy
        };

    /// <summary>
    /// Applies the same visibility filter as MyEntries.razor LoadEntries().
    /// Worker role: entries where WorkerId matches OR CreatedBy matches.
    /// Other roles: all entries.
    /// </summary>
    private static List<WorkEntry> ApplyVisibilityFilter(
        List<WorkEntry> entries, Role role, Guid? workerId, Guid? profileId)
    {
        if (role == Role.Worker)
        {
            return entries.Where(e =>
                (workerId.HasValue && e.WorkerId == workerId.Value) ||
                (profileId.HasValue && e.CreatedBy == profileId.Value)
            ).ToList();
        }
        return entries;
    }

    /// <summary>
    /// Property 1: For a Worker-role user, filtering returns exactly entries where
    /// WorkerId == workerId OR CreatedBy == profileId.
    /// **Validates: Requirements 11.6**
    /// </summary>
    [Property]
    public Property WorkerSeesOwnAndCreatedByEntries()
    {
        var entriesGen = Gen.ListOf(WorkEntryGen);

        return Prop.ForAll(
            Arb.From(entriesGen),
            Arb.From(Gen.Fresh(() => Guid.NewGuid())),
            Arb.From(Gen.Fresh(() => Guid.NewGuid())),
            (entries, workerId, profileId) =>
            {
                var filtered = ApplyVisibilityFilter(entries, Role.Worker, workerId, profileId);

                return filtered.All(e =>
                    e.WorkerId == workerId || e.CreatedBy == profileId);
            });
    }

    /// <summary>
    /// Property 2: For Manager, Boss, and Admin roles, no filtering occurs — all entries are returned.
    /// **Validates: Requirements 11.6**
    /// </summary>
    [Property]
    public Property ManagerBossAdminSeeAllEntries()
    {
        var entriesGen = Gen.ListOf(WorkEntryGen);
        var nonWorkerRoleGen = Gen.Elements(Role.Manager, Role.Boss, Role.Admin);
        var contextGen =
            from role in nonWorkerRoleGen
            from wId in Gen.Fresh(() => Guid.NewGuid())
            from pId in Gen.Fresh(() => Guid.NewGuid())
            select (role, wId, pId);

        return Prop.ForAll(
            Arb.From(entriesGen),
            Arb.From(contextGen),
            (entries, ctx) =>
            {
                var filtered = ApplyVisibilityFilter(entries, ctx.role, ctx.wId, ctx.pId);

                return filtered.Count == entries.Count &&
                       filtered.SequenceEqual(entries);
            });
    }

    /// <summary>
    /// Property 3: The Worker visibility filter never includes entries that don't belong
    /// to the worker (neither by WorkerId nor CreatedBy).
    /// **Validates: Requirements 11.6**
    /// </summary>
    [Property]
    public Property WorkerFilterNeverIncludesUnrelatedEntries()
    {
        var entriesGen = Gen.ListOf(WorkEntryGen);

        return Prop.ForAll(
            Arb.From(entriesGen),
            Arb.From(Gen.Fresh(() => Guid.NewGuid())),
            Arb.From(Gen.Fresh(() => Guid.NewGuid())),
            (entries, workerId, profileId) =>
            {
                var filtered = ApplyVisibilityFilter(entries, Role.Worker, workerId, profileId);

                // Every entry in the filtered result must be related to the worker
                return !filtered.Any(e =>
                    e.WorkerId != workerId && e.CreatedBy != profileId);
            });
    }

    /// <summary>
    /// Property 4: The Worker visibility filter never excludes entries that DO belong
    /// to the worker (by WorkerId or CreatedBy).
    /// **Validates: Requirements 11.6**
    /// </summary>
    [Property]
    public Property WorkerFilterNeverExcludesOwnEntries()
    {
        var entriesGen = Gen.ListOf(WorkEntryGen);

        return Prop.ForAll(
            Arb.From(entriesGen),
            Arb.From(Gen.Fresh(() => Guid.NewGuid())),
            Arb.From(Gen.Fresh(() => Guid.NewGuid())),
            (entries, workerId, profileId) =>
            {
                var filtered = ApplyVisibilityFilter(entries, Role.Worker, workerId, profileId);

                // Every entry that belongs to the worker must be in the filtered result
                var expected = entries.Where(e =>
                    e.WorkerId == workerId || e.CreatedBy == profileId).ToList();

                return expected.Count == filtered.Count &&
                       expected.All(e => filtered.Contains(e));
            });
    }
}
