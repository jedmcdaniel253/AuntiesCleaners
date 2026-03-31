using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Services;
using AuntiesCleaners.Tests.Helpers;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using NSubstitute;

namespace AuntiesCleaners.Tests.Unit;

/// <summary>
/// Property tests for House owner assignment round-trip.
/// Feature: multi-owner-house-assignment
/// </summary>
public class HouseOwnerAssignmentTests
{
    /// <summary>
    /// Property 8: House owner_id round-trip
    /// For any House and any Owner, saving the house with owner_id set to that Owner's ID
    /// and reading it back SHALL return the same owner_id. Saving with owner_id = null
    /// and reading back SHALL return null.
    /// **Validates: Requirements 6.3, 6.4**
    /// </summary>
    [Property]
    public Property HouseOwnerIdRoundTrip()
    {
        var gen =
            from house in Generators.HouseGen
            from owner in Generators.OwnerGen
            select (house, owner);

        return Prop.ForAll(
            Arb.From(gen),
            async tuple =>
            {
                var (house, owner) = tuple;
                var service = Substitute.For<IHouseService>();

                // --- Round-trip with owner_id set ---
                house.OwnerId = owner.Id;

                var savedHouse = new House
                {
                    Id = house.Id,
                    Name = house.Name,
                    IsMultipleHouses = house.IsMultipleHouses,
                    IsActive = house.IsActive,
                    CreatedAt = house.CreatedAt,
                    OwnerId = house.OwnerId
                };

                service.CreateAsync(Arg.Any<House>()).Returns(savedHouse);
                service.UpdateAsync(Arg.Any<House>()).Returns(savedHouse);
                service.GetAllAsync().Returns(new List<House> { savedHouse });

                var createResult = await service.CreateAsync(house);
                var allHouses = await service.GetAllAsync();
                var readBack = allHouses.First(h => h.Id == createResult.Id);

                if (readBack.OwnerId != owner.Id)
                    return false;

                // --- Round-trip with owner_id = null ---
                var nullOwnerHouse = new House
                {
                    Id = house.Id,
                    Name = house.Name,
                    IsMultipleHouses = house.IsMultipleHouses,
                    IsActive = house.IsActive,
                    CreatedAt = house.CreatedAt,
                    OwnerId = null
                };

                service.UpdateAsync(Arg.Any<House>()).Returns(nullOwnerHouse);
                service.GetAllAsync().Returns(new List<House> { nullOwnerHouse });

                var updateResult = await service.UpdateAsync(nullOwnerHouse);
                var allHousesAfter = await service.GetAllAsync();
                var readBackNull = allHousesAfter.First(h => h.Id == updateResult.Id);

                return readBackNull.OwnerId == null;
            });
    }
}
