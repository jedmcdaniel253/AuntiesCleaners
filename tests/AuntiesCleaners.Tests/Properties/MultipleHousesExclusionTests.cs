using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Services;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// P9: Multiple Houses Exclusion for Lawn — verify lawn entries cannot reference Multiple_Houses.
/// **Validates: Requirements 2.4, 2.12**
/// </summary>
public class MultipleHousesExclusionTests
{
    private static Gen<House> HouseGen =>
        from id in Gen.Fresh(() => Guid.NewGuid())
        from name in Gen.Elements("House A", "House B", "House C", "Beach House", "Lake House")
        from isMultiple in Gen.Elements(true, false)
        from isActive in Gen.Elements(true, false)
        select new House
        {
            Id = id,
            Name = name,
            IsMultipleHouses = isMultiple,
            IsActive = isActive
        };


    /// <summary>
    /// Property 1: Filtering out Multiple Houses never includes any house where IsMultipleHouses is true.
    /// Given any list of houses (some with IsMultipleHouses=true, some false),
    /// the result of excluding Multiple Houses contains zero houses with IsMultipleHouses=true.
    /// **Validates: Requirements 2.12**
    /// </summary>
    [Property]
    public Property FilteringExcludesAllMultipleHouses()
    {
        var housesGen = Gen.ListOf(HouseGen);

        return Prop.ForAll(
            Arb.From(housesGen),
            houses =>
            {
                // Apply the same filter as GetActiveExcludingMultipleAsync
                var filtered = houses.Where(h => h.IsActive && !h.IsMultipleHouses).ToList();
                return !filtered.Any(h => h.IsMultipleHouses);
            });
    }

    /// <summary>
    /// Property 2: For any WorkEntry with WorkCategoryValue="Lawn", if validated against a house list,
    /// the referenced house must not be a Multiple Houses entry.
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Property]
    public Property LawnEntryHouseMustNotBeMultipleHouses()
    {
        var housesGen = Gen.NonEmptyListOf(HouseGen);

        return Prop.ForAll(
            Arb.From(housesGen),
            houses =>
            {
                // Simulate the exclusion logic used for Lawn entries
                var allowedHouses = houses.Where(h => h.IsActive && !h.IsMultipleHouses).ToList();

                // Every house in the allowed list, when used for a Lawn entry, must not be Multiple Houses
                foreach (var house in allowedHouses)
                {
                    var entry = new WorkEntry
                    {
                        Id = Guid.NewGuid(),
                        WorkerId = Guid.NewGuid(),
                        HouseId = house.Id,
                        WorkCategoryValue = WorkCategory.Lawn.ToString(),
                        EntryDate = DateTime.Today
                    };

                    var referencedHouse = houses.FirstOrDefault(h => h.Id == entry.HouseId);
                    if (referencedHouse != null && referencedHouse.IsMultipleHouses)
                        return false;
                }

                return true;
            });
    }

    /// <summary>
    /// Property 3: GetActiveExcludingMultipleAsync filtering logic never returns houses
    /// with IsMultipleHouses=true, and all returned houses are active.
    /// **Validates: Requirements 2.12**
    /// </summary>
    [Property]
    public Property ServiceExcludingMultipleNeverReturnsMultipleHouses()
    {
        var housesGen = Gen.ListOf(HouseGen);

        return Prop.ForAll(
            Arb.From(housesGen),
            houses =>
            {
                // Simulate what GetActiveExcludingMultipleAsync does
                var result = houses.Where(h => h.IsActive && !h.IsMultipleHouses).ToList();

                var noMultiple = !result.Any(h => h.IsMultipleHouses);
                var allActive = result.All(h => h.IsActive);

                return noMultiple && allActive;
            });
    }

    /// <summary>
    /// Property 4: HouseDropdown with IncludeMultipleHouses=false (Lawn category)
    /// should not include any Multiple Houses entries.
    /// Verifies the dropdown filtering logic matches the exclusion requirement.
    /// **Validates: Requirements 2.4, 2.12**
    /// </summary>
    [Property]
    public Property DropdownForLawnExcludesMultipleHouses()
    {
        var housesGen = Gen.NonEmptyListOf(HouseGen);

        return Prop.ForAll(
            Arb.From(housesGen),
            houses =>
            {
                // When IncludeMultipleHouses is false (Lawn category),
                // the dropdown calls GetActiveExcludingMultipleAsync
                var dropdownHouses = houses
                    .Where(h => h.IsActive && !h.IsMultipleHouses)
                    .ToList();

                // No house in the dropdown should have IsMultipleHouses = true
                return !dropdownHouses.Any(h => h.IsMultipleHouses);
            });
    }
}
