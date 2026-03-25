using AuntiesCleaners.Client.Models;
using FsCheck;
using FsCheck.Fluent;

namespace AuntiesCleaners.Tests.Helpers;

/// <summary>
/// A simple date range record with From &lt;= To.
/// </summary>
public record DateRange(DateTime From, DateTime To);

/// <summary>
/// Reusable FsCheck generators for all domain models.
/// Uses Gen&lt;T&gt; and Arb.From&lt;T&gt;() patterns consistent with FsCheck 3.x Fluent API.
/// All generators respect domain invariants (positive rates, category-specific fields, etc.).
/// </summary>
public static class Generators
{
    // ── Primitives ──────────────────────────────────────────────────

    public static Gen<Guid> GuidGen => Gen.Fresh(() => Guid.NewGuid());

    public static Gen<DateTime> ReasonableDateGen =>
        Gen.Choose(0, 730).Select(offset => DateTime.Today.AddDays(-offset));

    public static Gen<string> NonEmptyNameGen =>
        Gen.Elements(
            "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank",
            "Grace", "Hank", "Ivy", "Jack", "Karen", "Leo");

    public static Gen<string> HouseNameGen =>
        Gen.Elements(
            "Beach House", "Lake House", "Mountain Cabin", "Downtown Apt",
            "Riverside", "Hilltop", "Sunset Villa", "Oak Lodge");

    public static Gen<string> BusinessNameGen =>
        Gen.Elements(
            "Home Depot", "Lowes", "Walmart", "Target", "Costco",
            "ACE Hardware", "Dollar General", "Staples");

    public static Gen<string?> OptionalPhoneGen =>
        Gen.Elements<string?>(null, "555-0100", "555-0200", "555-0300", "555-0400");

    public static Gen<string?> OptionalNotesGen =>
        Gen.Elements<string?>(null, "Guest arriving", "Early checkout", "VIP", "");

    /// <summary>Strictly positive decimal (0.01 – 1000.00).</summary>
    public static Gen<decimal> PositiveDecimalGen =>
        Gen.Choose(1, 100000).Select(i => (decimal)i / 100m);

    /// <summary>Non-negative decimal (0.00 – 1000.00).</summary>
    public static Gen<decimal> NonNegativeDecimalGen =>
        Gen.Choose(0, 100000).Select(i => (decimal)i / 100m);

    /// <summary>Positive int (1 – 50).</summary>
    public static Gen<int> PositiveIntGen => Gen.Choose(1, 50);

    public static Gen<WorkCategory> WorkCategoryGen =>
        Gen.Elements(WorkCategory.Cleaning, WorkCategory.Laundry, WorkCategory.Maintenance, WorkCategory.Lawn);

    // ── Worker ──────────────────────────────────────────────────────

    public static Gen<Worker> WorkerGen =>
        from id in GuidGen
        from name in NonEmptyNameGen
        from phone in OptionalPhoneGen
        from isActive in Gen.Elements(true, false)
        from createdAt in ReasonableDateGen
        select new Worker
        {
            Id = id,
            Name = name,
            Phone = phone,
            IsActive = isActive,
            CreatedAt = createdAt
        };

    public static Gen<Worker> ActiveWorkerGen =>
        WorkerGen.Select(w => { w.IsActive = true; return w; });

    // ── House ───────────────────────────────────────────────────────

    public static Gen<House> HouseGen =>
        from id in GuidGen
        from name in HouseNameGen
        from isMultiple in Gen.Elements(true, false)
        from isActive in Gen.Elements(true, false)
        from createdAt in ReasonableDateGen
        select new House
        {
            Id = id,
            Name = name,
            IsMultipleHouses = isMultiple,
            IsActive = isActive,
            CreatedAt = createdAt
        };

    /// <summary>A regular house (not Multiple Houses, active).</summary>
    public static Gen<House> RegularActiveHouseGen =>
        from id in GuidGen
        from name in HouseNameGen
        from createdAt in ReasonableDateGen
        select new House
        {
            Id = id,
            Name = name,
            IsMultipleHouses = false,
            IsActive = true,
            CreatedAt = createdAt
        };

    // ── WorkEntry (category-specific) ───────────────────────────────

    /// <summary>
    /// Cleaning entry: requires HoursBilled &gt; 0, house may be Multiple Houses.
    /// </summary>
    public static Gen<WorkEntry> CleaningEntryGen =>
        from id in GuidGen
        from workerId in GuidGen
        from houseId in GuidGen
        from date in ReasonableDateGen
        from hours in PositiveDecimalGen
        from createdBy in GuidGen
        from createdAt in ReasonableDateGen
        select new WorkEntry
        {
            Id = id,
            WorkerId = workerId,
            HouseId = houseId,
            WorkCategoryValue = WorkCategory.Cleaning.ToString(),
            EntryDate = date,
            HoursBilled = hours,
            NumberOfLoads = null,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };

    /// <summary>
    /// Laundry entry: requires NumberOfLoads &gt; 0, house may be Multiple Houses.
    /// </summary>
    public static Gen<WorkEntry> LaundryEntryGen =>
        from id in GuidGen
        from workerId in GuidGen
        from houseId in GuidGen
        from date in ReasonableDateGen
        from loads in PositiveIntGen
        from createdBy in GuidGen
        from createdAt in ReasonableDateGen
        select new WorkEntry
        {
            Id = id,
            WorkerId = workerId,
            HouseId = houseId,
            WorkCategoryValue = WorkCategory.Laundry.ToString(),
            EntryDate = date,
            HoursBilled = null,
            NumberOfLoads = loads,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };

    /// <summary>
    /// Maintenance entry: requires HoursBilled &gt; 0, house may be Multiple Houses.
    /// </summary>
    public static Gen<WorkEntry> MaintenanceEntryGen =>
        from id in GuidGen
        from workerId in GuidGen
        from houseId in GuidGen
        from date in ReasonableDateGen
        from hours in PositiveDecimalGen
        from createdBy in GuidGen
        from createdAt in ReasonableDateGen
        select new WorkEntry
        {
            Id = id,
            WorkerId = workerId,
            HouseId = houseId,
            WorkCategoryValue = WorkCategory.Maintenance.ToString(),
            EntryDate = date,
            HoursBilled = hours,
            NumberOfLoads = null,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };

    /// <summary>
    /// Lawn entry: no HoursBilled or NumberOfLoads, house must NOT be Multiple Houses.
    /// Use with RegularActiveHouseGen to ensure the house constraint.
    /// </summary>
    public static Gen<WorkEntry> LawnEntryGen =>
        from id in GuidGen
        from workerId in GuidGen
        from houseId in GuidGen
        from date in ReasonableDateGen
        from createdBy in GuidGen
        from createdAt in ReasonableDateGen
        select new WorkEntry
        {
            Id = id,
            WorkerId = workerId,
            HouseId = houseId,
            WorkCategoryValue = WorkCategory.Lawn.ToString(),
            EntryDate = date,
            HoursBilled = null,
            NumberOfLoads = null,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };

    /// <summary>
    /// Generates a valid WorkEntry for any category with correct category-specific fields.
    /// </summary>
    public static Gen<WorkEntry> WorkEntryGen =>
        Gen.OneOf(CleaningEntryGen, LaundryEntryGen, MaintenanceEntryGen, LawnEntryGen);

    // ── MiscellaneousEntry ──────────────────────────────────────────

    /// <summary>
    /// ChargeAmount &gt; 0, PayAmount &gt;= 0, non-empty description.
    /// </summary>
    public static Gen<MiscellaneousEntry> MiscellaneousEntryGen =>
        from id in GuidGen
        from workerId in GuidGen
        from houseId in Gen.OneOf(
            GuidGen.Select(g => (Guid?)g),
            Gen.Constant((Guid?)null))
        from date in ReasonableDateGen
        from descIdx in Gen.Choose(1, 500)
        from charge in PositiveDecimalGen
        from pay in NonNegativeDecimalGen
        from createdBy in GuidGen
        from createdAt in ReasonableDateGen
        select new MiscellaneousEntry
        {
            Id = id,
            WorkerId = workerId,
            HouseId = houseId,
            EntryDate = date,
            Description = $"Misc job #{descIdx}",
            ChargeAmount = charge,
            PayAmount = pay,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };

    // ── Receipt ─────────────────────────────────────────────────────

    /// <summary>
    /// Amount &gt; 0, non-empty business name and photo URL.
    /// </summary>
    public static Gen<Receipt> ReceiptGen =>
        from id in GuidGen
        from workerId in GuidGen
        from date in ReasonableDateGen
        from bizName in BusinessNameGen
        from amount in PositiveDecimalGen
        from reimbursable in Gen.Elements(true, false)
        from photoIdx in Gen.Choose(1, 9999)
        from createdBy in GuidGen
        from createdAt in ReasonableDateGen
        select new Receipt
        {
            Id = id,
            WorkerId = workerId,
            ReceiptDate = date,
            BusinessName = bizName,
            Amount = amount,
            IsReimbursable = reimbursable,
            PhotoUrl = $"https://storage.example.com/receipts/{photoIdx}.jpg",
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };

    // ── Rate ────────────────────────────────────────────────────────

    /// <summary>
    /// RateCharged &gt; 0, RatePaid &gt;= 0, valid work category.
    /// </summary>
    public static Gen<Rate> RateGen =>
        from id in GuidGen
        from category in WorkCategoryGen
        from workerId in Gen.OneOf(
            GuidGen.Select(g => (Guid?)g),
            Gen.Constant((Guid?)null))
        from charged in PositiveDecimalGen
        from paid in NonNegativeDecimalGen
        from updatedAt in ReasonableDateGen
        select new Rate
        {
            Id = id,
            WorkCategoryValue = category.ToString(),
            WorkerId = workerId,
            RateCharged = charged,
            RatePaid = paid,
            UpdatedAt = updatedAt
        };

    // ── LawnHouseRate ───────────────────────────────────────────────

    /// <summary>
    /// RateCharged &gt; 0, RatePaid &gt;= 0.
    /// </summary>
    public static Gen<LawnHouseRate> LawnHouseRateGen =>
        from id in GuidGen
        from houseId in GuidGen
        from workerId in Gen.OneOf(
            GuidGen.Select(g => (Guid?)g),
            Gen.Constant((Guid?)null))
        from charged in PositiveDecimalGen
        from paid in NonNegativeDecimalGen
        from updatedAt in ReasonableDateGen
        select new LawnHouseRate
        {
            Id = id,
            HouseId = houseId,
            WorkerId = workerId,
            RateCharged = charged,
            RatePaid = paid,
            UpdatedAt = updatedAt
        };

    // ── TurnoverEvent ───────────────────────────────────────────────

    public static Gen<TurnoverEvent> TurnoverEventGen =>
        from id in GuidGen
        from houseId in GuidGen
        from date in ReasonableDateGen
        from isCheckout in Gen.Elements(true, false)
        from isCheckin in Gen.Elements(true, false)
        from notes in OptionalNotesGen
        from createdBy in GuidGen
        from createdAt in ReasonableDateGen
        select new TurnoverEvent
        {
            Id = id,
            HouseId = houseId,
            EventDate = date,
            IsCheckout = isCheckout,
            IsCheckin = isCheckin,
            Notes = notes,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };

    // ── DateRange ───────────────────────────────────────────────────

    /// <summary>
    /// Generates a DateRange where From &lt;= To, spanning 0–365 days.
    /// </summary>
    public static Gen<DateRange> DateRangeGen =>
        from startOffset in Gen.Choose(0, 730)
        from span in Gen.Choose(0, 365)
        let start = DateTime.Today.AddDays(-startOffset)
        let end = start.AddDays(span)
        select new DateRange(start, end);

    // ── TabConfiguration ────────────────────────────────────────────

    public static Gen<TabConfiguration> TabConfigurationGen =>
        from id in GuidGen
        from profileId in GuidGen
        from tabName in Gen.Elements(
            "Cleaning", "Laundry", "Maintenance", "Lawn", "Receipts",
            "Calendar", "Mow List", "My Entries", "Daily Summary",
            "Rates", "Billing", "Worker Pay", "Owner",
            "Workers", "Houses", "Users", "Tab Config")
        from order in Gen.Choose(0, 20)
        from visible in Gen.Elements(true, false)
        select new TabConfiguration
        {
            Id = id,
            UserProfileId = profileId,
            TabName = tabName,
            DisplayOrder = order,
            IsVisible = visible
        };
}
