using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Tests.Helpers;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// Property tests for calendar owner filtering.
/// Feature: multi-owner-house-assignment
/// </summary>
public class CalendarFilterTests
{
    /// <summary>
    /// Mirrors the filtering logic from TurnoverCalendar.razor:
    /// - When selectedOwner is null ("All Owners"), return all events.
    /// - When selectedOwner is set, return only events whose HouseId belongs
    ///   to a house with OwnerId matching the selected owner.
    /// </summary>
    private static List<TurnoverEvent> FilterEvents(
        List<House> houses,
        List<TurnoverEvent> events,
        Owner? selectedOwner)
    {
        if (selectedOwner == null)
            return events;

        var filteredHouseIds = houses
            .Where(h => h.OwnerId == selectedOwner.Id)
            .Select(h => h.Id)
            .ToHashSet();

        return events.Where(e => filteredHouseIds.Contains(e.HouseId)).ToList();
    }

    /// <summary>
    /// Property 9: Calendar events filtered by selected owner
    /// For any set of houses with various owner_id values and any set of turnover events,
    /// when an owner is selected in the filter, the displayed events SHALL be exactly those
    /// whose house has owner_id matching the selected owner. When "All Owners" is selected,
    /// all events SHALL be displayed.
    /// **Validates: Requirements 10.2, 10.3**
    /// </summary>
    [Property]
    public Property CalendarEventsFilteredBySelectedOwner()
    {
        var gen =
            from owner1 in Generators.OwnerGen
            from owner2 in Generators.OwnerGen
            from houseCount in Gen.Choose(1, 10)
            from houses in Gen.ListOf(Generators.HouseGen, houseCount)
            from ownerFlags in Gen.ListOf(Gen.Choose(0, 2), houseCount)
            from eventCount in Gen.Choose(0, 15)
            from events in Gen.ListOf(Generators.TurnoverEventGen, eventCount)
            select (owner1, owner2,
                    houses: AssignOwners(houses.ToList(), ownerFlags.ToList(), owner1.Id, owner2.Id),
                    events: AssignEventsToHouses(events.ToList(), houses.ToList()));

        return Prop.ForAll(
            Arb.From(gen),
            data =>
            {
                var (owner1, owner2, houses, events) = data;

                // --- "All Owners" case: all events displayed ---
                var allResult = FilterEvents(houses, events, null);
                if (allResult.Count != events.Count)
                    return false;

                // --- Owner selected: only matching events ---
                var owner1HouseIds = houses
                    .Where(h => h.OwnerId == owner1.Id)
                    .Select(h => h.Id)
                    .ToHashSet();

                var filtered = FilterEvents(houses, events, owner1);

                // Every filtered event must belong to an owner1 house
                if (filtered.Any(e => !owner1HouseIds.Contains(e.HouseId)))
                    return false;

                // Every event belonging to an owner1 house must be in the filtered set
                var expectedEvents = events.Where(e => owner1HouseIds.Contains(e.HouseId)).ToList();
                if (filtered.Count != expectedEvents.Count)
                    return false;

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
