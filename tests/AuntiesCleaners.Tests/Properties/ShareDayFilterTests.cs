using AuntiesCleaners.Client.Helpers;
using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Tests.Helpers;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// Property tests for share day text owner filtering.
/// Feature: multi-owner-house-assignment
/// </summary>
public class ShareDayFilterTests
{
    /// <summary>
    /// Property 10: Share day text filtered by selected owner
    /// For any day's turnover events and any selected owner, the share text SHALL contain
    /// entries only for houses where owner_id matches the selected owner's ID. When "All Owners"
    /// is selected, the share text SHALL contain all events for that day.
    /// **Validates: Requirements 11.1, 11.2**
    /// </summary>
    [Property]
    public Property ShareDayTextFilteredBySelectedOwner()
    {
        var gen =
            from owner in Generators.OwnerGen
            from otherOwner in Generators.OwnerGen
            from houseCount in Gen.Choose(1, 8)
            from houses in Gen.ListOf(Generators.HouseGen, houseCount)
            from ownerFlags in Gen.ListOf(Gen.Choose(0, 2), houseCount)
            from eventCount in Gen.Choose(1, 10)
            from events in Gen.ListOf(Generators.TurnoverEventGen, eventCount)
            select (owner, otherOwner,
                    houses: AssignOwners(houses.ToList(), ownerFlags.ToList(), owner.Id, otherOwner.Id),
                    events: AssignEventsToHouses(events.ToList(), houses.ToList()));

        return Prop.ForAll(
            Arb.From(gen),
            data =>
            {
                var (owner, otherOwner, houses, events) = data;
                var dayLabel = "Today (1/1/25)";

                // --- Owner selected: ComposeOwnerText filters to owner's houses ---
                var ownerText = ShareTextComposer.ComposeOwnerText(
                    dayLabel, owner.Name, events, houses, owner.Id);

                var ownerHouseIds = houses
                    .Where(h => h.OwnerId == owner.Id)
                    .Select(h => h.Id)
                    .ToHashSet();

                var ownerEvents = events.Where(e => ownerHouseIds.Contains(e.HouseId)).ToList();

                if (ownerEvents.Count == 0)
                {
                    // When no events match, ComposeOwnerText returns empty string
                    if (!string.IsNullOrEmpty(ownerText))
                        return false;
                }
                else
                {
                    // Text must contain entries for each matching event's house
                    var houseLookup = houses.ToDictionary(h => h.Id, h => h.Name);
                    foreach (var e in ownerEvents)
                    {
                        if (!ownerText.Contains(houseLookup[e.HouseId]))
                            return false;
                    }

                    // Text must NOT contain entries for non-owner houses that have events
                    var nonOwnerHouseNames = houses
                        .Where(h => h.OwnerId != owner.Id)
                        .Where(h => events.Any(e => e.HouseId == h.Id))
                        .Select(h => h.Name)
                        .Distinct()
                        .ToList();

                    // Only check house names that don't collide with owner house names
                    var ownerHouseNames = houses
                        .Where(h => ownerHouseIds.Contains(h.Id))
                        .Select(h => h.Name)
                        .ToHashSet();

                    foreach (var name in nonOwnerHouseNames)
                    {
                        if (!ownerHouseNames.Contains(name) && ownerText.Contains(name))
                            return false;
                    }
                }

                // --- "All Owners": ComposeWorkerText includes all events ---
                var houseLookupAll = houses.ToDictionary(h => h.Id, h => h.Name);
                var allText = ShareTextComposer.ComposeWorkerText(
                    dayLabel, events, houseId => houseLookupAll.GetValueOrDefault(houseId, "Unknown"));

                // All events' house names must appear in the text
                foreach (var e in events)
                {
                    var houseName = houseLookupAll.GetValueOrDefault(e.HouseId, "Unknown");
                    if (!allText.Contains(houseName))
                        return false;
                }

                return true;
            });
    }

    /// <summary>
    /// Assigns OwnerId to houses based on flags:
    /// 0 = owner1, 1 = owner2, 2 = null (no owner).
    /// </summary>
    private static List<House> AssignOwners(
        List<House> houses, List<int> flags,
        Guid owner1Id, Guid owner2Id)
    {
        for (int i = 0; i < houses.Count; i++)
        {
            houses[i].OwnerId = flags[i] switch
            {
                0 => owner1Id,
                1 => owner2Id,
                _ => null
            };
        }
        return houses;
    }

    /// <summary>
    /// Assigns each event's HouseId to a random house from the list.
    /// </summary>
    private static List<TurnoverEvent> AssignEventsToHouses(
        List<TurnoverEvent> events, List<House> houses)
    {
        if (houses.Count == 0) return events;
        var random = new Random(42);
        foreach (var e in events)
        {
            e.HouseId = houses[random.Next(houses.Count)].Id;
        }
        return events;
    }
}
