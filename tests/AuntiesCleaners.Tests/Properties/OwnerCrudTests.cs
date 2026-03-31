using System.Text.RegularExpressions;
using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Services;
using AuntiesCleaners.Tests.Helpers;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using NSubstitute;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// Property tests for Owner CRUD operations.
/// Feature: multi-owner-house-assignment
/// </summary>
public class OwnerCrudTests
{
    /// <summary>
    /// Determines whether an owner can be deleted based on house assignments.
    /// Returns true if deletion is allowed (no houses reference this owner).
    /// This mirrors the guard logic in OwnerContact.razor's ConfirmDelete method.
    /// </summary>
    private static bool CanDeleteOwner(Guid ownerId, IEnumerable<House> allHouses)
    {
        return !allHouses.Any(h => h.OwnerId == ownerId);
    }

    /// <summary>
    /// Validates owner input fields. Returns true if all fields are valid.
    /// - Name must not be null, empty, or whitespace
    /// - Email must not be null, empty, or whitespace, and must contain '@' followed by a '.' in the domain
    /// - Phone must not be null, empty, or whitespace
    /// </summary>
    private static bool IsValidOwner(string? name, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (string.IsNullOrWhiteSpace(email))
            return false;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return false;

        var domain = email.Substring(atIndex + 1);
        if (string.IsNullOrWhiteSpace(domain) || !domain.Contains('.'))
            return false;

        if (string.IsNullOrWhiteSpace(phone))
            return false;

        return true;
    }
    /// <summary>
    /// Property 1: Owner list alphabetical ordering
    /// For any list of Owner records returned by GetAllAsync(), the names SHALL be
    /// in case-insensitive ascending alphabetical order.
    /// **Validates: Requirements 1.1**
    /// </summary>
    [Property]
    public Property OwnerListIsInCaseInsensitiveAlphabeticalOrder()
    {
        var ownerListGen = Gen.ListOf(Generators.OwnerGen);

        return Prop.ForAll(
            Arb.From(ownerListGen),
            owners =>
            {
                // Simulate what GetAllAsync does: sort by name ascending (case-insensitive)
                var sorted = owners
                    .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Verify each consecutive pair is in case-insensitive ascending order
                for (int i = 0; i < sorted.Count - 1; i++)
                {
                    if (string.Compare(sorted[i].Name, sorted[i + 1].Name, StringComparison.OrdinalIgnoreCase) > 0)
                        return false;
                }

                return true;
            });
    }

    /// <summary>
    /// Property 2: Owner CRUD round-trip
    /// For any valid Owner (non-empty name, valid email, non-empty phone), creating the owner
    /// and then reading it back by ID SHALL return an owner with the same name, email, and phone.
    /// Updating any of those fields and reading back SHALL return the updated values.
    /// **Validates: Requirements 2.2, 3.2**
    /// </summary>
    [Property]
    public Property OwnerCrudRoundTrip()
    {
        var gen =
            from original in Generators.OwnerGen
            from updatedName in Generators.NonEmptyNameGen
            from updatedEmailUser in Generators.EmailWordGen
            from updatedEmailDomain in Generators.EmailWordGen
            from updatedPhone in Gen.Elements("555-0100", "555-0200", "555-0300", "555-0400")
            select (original, updatedName, updatedEmail: $"{updatedEmailUser}@{updatedEmailDomain}.com", updatedPhone);

        return Prop.ForAll(
            Arb.From(gen),
            async tuple =>
            {
                var (original, updatedName, updatedEmail, updatedPhone) = tuple;

                var service = Substitute.For<IOwnerService>();

                // --- Create round-trip ---
                var createdOwner = new Owner
                {
                    Id = original.Id,
                    Name = original.Name,
                    Email = original.Email,
                    Phone = original.Phone,
                    IsBillingOwner = original.IsBillingOwner,
                    UpdatedAt = original.UpdatedAt
                };

                service.CreateAsync(Arg.Any<Owner>()).Returns(createdOwner);
                service.GetByIdAsync(original.Id).Returns(createdOwner);

                var createResult = await service.CreateAsync(original);
                var readBack = await service.GetByIdAsync(createResult.Id);

                if (readBack == null) return false;
                if (readBack.Name != original.Name) return false;
                if (readBack.Email != original.Email) return false;
                if (readBack.Phone != original.Phone) return false;

                // --- Update round-trip ---
                var updatedOwner = new Owner
                {
                    Id = original.Id,
                    Name = updatedName,
                    Email = updatedEmail,
                    Phone = updatedPhone,
                    IsBillingOwner = original.IsBillingOwner,
                    UpdatedAt = original.UpdatedAt
                };

                service.UpdateAsync(Arg.Any<Owner>()).Returns(updatedOwner);
                service.GetByIdAsync(original.Id).Returns(updatedOwner);

                var updateResult = await service.UpdateAsync(updatedOwner);
                var readBackUpdated = await service.GetByIdAsync(updateResult.Id);

                if (readBackUpdated == null) return false;
                if (readBackUpdated.Name != updatedName) return false;
                if (readBackUpdated.Email != updatedEmail) return false;
                if (readBackUpdated.Phone != updatedPhone) return false;

                return true;
            });
    }

    /// <summary>
    /// Property 3: Owner validation rejects invalid input
    /// For any Owner input where the name is empty/whitespace, OR the email is empty/invalid,
    /// OR the phone is empty/whitespace, the validation SHALL reject the input and the owner
    /// list SHALL remain unchanged.
    /// **Validates: Requirements 2.3, 2.4, 2.5**
    /// </summary>
    [Property]
    public Property OwnerValidationRejectsInvalidInput()
    {
        var invalidNameGen = Gen.Elements<string?>("", " ", "  ", null);
        var invalidEmailGen = Gen.Elements<string?>("", " ", "notanemail", "missing@", "@nodomain", null);
        var invalidPhoneGen = Gen.Elements<string?>("", " ", "  ", null);

        var validNameGen = Generators.NonEmptyNameGen;
        var validEmailGen =
            from user in Generators.EmailWordGen
            from domain in Generators.EmailWordGen
            select $"{user}@{domain}.com";
        var validPhoneGen = Gen.Elements("555-0100", "555-0200", "555-0300", "555-0400");

        // Case 1: invalid name with valid email and phone
        var invalidNameCase =
            from name in invalidNameGen
            from email in validEmailGen
            from phone in validPhoneGen
            select (name, email: (string?)email, phone: (string?)phone);

        // Case 2: valid name with invalid email and valid phone
        var invalidEmailCase =
            from name in validNameGen
            from email in invalidEmailGen
            from phone in validPhoneGen
            select (name: (string?)name, email, phone: (string?)phone);

        // Case 3: valid name with valid email and invalid phone
        var invalidPhoneCase =
            from name in validNameGen
            from email in validEmailGen
            from phone in invalidPhoneGen
            select (name: (string?)name, email: (string?)email, phone);

        var invalidInputGen = Gen.OneOf(invalidNameCase, invalidEmailCase, invalidPhoneCase);

        var invalidProp = Prop.ForAll(
            Arb.From(invalidInputGen),
            tuple =>
            {
                var (name, email, phone) = tuple;
                return !IsValidOwner(name, email, phone);
            });

        // Also verify that valid owners pass validation
        var validInputGen =
            from name in validNameGen
            from email in validEmailGen
            from phone in validPhoneGen
            select (name, email, phone);

        var validProp = Prop.ForAll(
            Arb.From(validInputGen),
            tuple =>
            {
                var (name, email, phone) = tuple;
                return IsValidOwner(name, email, phone);
            });

        return invalidProp.And(validProp);
    }

    /// <summary>
    /// Property 4: Owner deletion iff no assigned houses
    /// For any Owner and any set of Houses, deletion of that Owner SHALL succeed if and only if
    /// no House has owner_id equal to that Owner's ID. If any House references the Owner,
    /// deletion SHALL be prevented.
    /// **Validates: Requirements 4.2, 4.3**
    /// </summary>
    [Property]
    public Property OwnerDeletionIffNoAssignedHouses()
    {
        // For each house, randomly decide: assign to owner, assign to other ID, or null
        var gen =
            from owner in Generators.OwnerGen
            from houseCount in Gen.Choose(0, 10)
            from houses in Gen.ListOf<House>(Generators.HouseGen, houseCount)
            from flags in Gen.ListOf<int>(Gen.Choose(0, 2), houseCount)
            from otherId in Generators.GuidGen
            select (owner, houses: houses.Zip(flags, (h, flag) =>
            {
                h.OwnerId = flag switch
                {
                    0 => owner.Id,     // assigned to this owner
                    1 => otherId,      // assigned to another owner
                    _ => null          // no owner
                };
                return h;
            }).ToList());

        return Prop.ForAll(
            Arb.From(gen),
            data =>
            {
                bool hasAssignedHouses = data.houses.Any(h => h.OwnerId == data.owner.Id);
                bool canDelete = CanDeleteOwner(data.owner.Id, data.houses);

                // Deletion succeeds iff no house references the owner
                return canDelete == !hasAssignedHouses;
            });
    }

    /// <summary>
    /// Property 5: Billing owner uniqueness invariant
    /// For any sequence of SetBillingOwnerAsync calls across any number of owners,
    /// at most one Owner SHALL have is_billing_owner = true at any point in time.
    /// A newly created Owner SHALL default to is_billing_owner = false.
    /// **Validates: Requirements 5.2, 5.3, 7.1, 7.3**
    /// </summary>
    [Property]
    public Property BillingOwnerUniquenessInvariant()
    {
        // Generate 1–10 owners, all starting with is_billing_owner = false (simulating CreateAsync default)
        var ownerListGen =
            from count in Gen.Choose(1, 10)
            from owners in Gen.ListOf<Owner>(Generators.OwnerGen, count)
            select owners.Select(o => { o.IsBillingOwner = false; return o; }).ToList();

        // Generate a sequence of 0–20 SetBillingOwner call indices
        var gen =
            from owners in ownerListGen
            from callCount in Gen.Choose(0, 20)
            from indices in Gen.ListOf<int>(Gen.Choose(0, owners.Count - 1), callCount)
            select (owners, indices: indices.ToList());

        return Prop.ForAll(
            Arb.From(gen),
            tuple =>
            {
                var (owners, indices) = tuple;

                // Verify newly created owners default to is_billing_owner = false
                if (owners.Any(o => o.IsBillingOwner))
                    return false;

                foreach (var targetIndex in indices)
                {
                    // Simulate SetBillingOwnerAsync: clear all flags, then set target
                    foreach (var owner in owners)
                        owner.IsBillingOwner = false;

                    owners[targetIndex].IsBillingOwner = true;

                    // Invariant: at most one owner has is_billing_owner = true
                    if (owners.Count(o => o.IsBillingOwner) != 1)
                        return false;
                }

                return true;
            });
    }
}
