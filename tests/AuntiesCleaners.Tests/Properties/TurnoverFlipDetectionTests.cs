using AuntiesCleaners.Client.Models;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// P6: Turnover Calendar Flip Detection — verify Flip label when both checkout and checkin are true.
/// **Validates: Requirements 14.7**
/// </summary>
public class TurnoverFlipDetectionTests
{
    private static Gen<TurnoverEvent> TurnoverEventGen(bool isCheckout, bool isCheckin) =>
        from id in Gen.Fresh(() => Guid.NewGuid())
        from houseId in Gen.Fresh(() => Guid.NewGuid())
        from dayOffset in Gen.Choose(0, 365)
        from notes in Gen.Elements<string?>(null, "Guest arriving", "Early checkout", "VIP", "")
        select new TurnoverEvent
        {
            Id = id,
            HouseId = houseId,
            EventDate = DateTime.Today.AddDays(dayOffset),
            IsCheckout = isCheckout,
            IsCheckin = isCheckin,
            Notes = notes,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Property 1: When IsCheckout=true AND IsCheckin=true, IsFlip must be true and DisplayLabel must be "Flip".
    /// **Validates: Requirements 14.7**
    /// </summary>
    [Property]
    public Property BothCheckoutAndCheckinMeansFlip()
    {
        return Prop.ForAll(
            Arb.From(TurnoverEventGen(isCheckout: true, isCheckin: true)),
            evt => evt.IsFlip && evt.DisplayLabel == "Flip");
    }

    /// <summary>
    /// Property 2: When only IsCheckout=true (IsCheckin=false), IsFlip must be false and DisplayLabel must be "Out".
    /// **Validates: Requirements 14.7**
    /// </summary>
    [Property]
    public Property OnlyCheckoutMeansOut()
    {
        return Prop.ForAll(
            Arb.From(TurnoverEventGen(isCheckout: true, isCheckin: false)),
            evt => !evt.IsFlip && evt.DisplayLabel == "Out");
    }

    /// <summary>
    /// Property 3: When only IsCheckin=true (IsCheckout=false), IsFlip must be false and DisplayLabel must be "In".
    /// **Validates: Requirements 14.7**
    /// </summary>
    [Property]
    public Property OnlyCheckinMeansIn()
    {
        return Prop.ForAll(
            Arb.From(TurnoverEventGen(isCheckout: false, isCheckin: true)),
            evt => !evt.IsFlip && evt.DisplayLabel == "In");
    }

    /// <summary>
    /// Property 4: IsFlip is always equivalent to (IsCheckout AND IsCheckin) for any combination of boolean values.
    /// **Validates: Requirements 14.7**
    /// </summary>
    [Property]
    public Property IsFlipEquivalentToCheckoutAndCheckin()
    {
        var gen =
            from isCheckout in Gen.Elements(true, false)
            from isCheckin in Gen.Elements(true, false)
            from id in Gen.Fresh(() => Guid.NewGuid())
            from houseId in Gen.Fresh(() => Guid.NewGuid())
            select new TurnoverEvent
            {
                Id = id,
                HouseId = houseId,
                EventDate = DateTime.Today,
                IsCheckout = isCheckout,
                IsCheckin = isCheckin,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

        return Prop.ForAll(
            Arb.From(gen),
            evt => evt.IsFlip == (evt.IsCheckout && evt.IsCheckin));
    }
}
