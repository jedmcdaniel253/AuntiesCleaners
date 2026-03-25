using AuntiesCleaners.Client.Services;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// P3: Rate Validation Invariant — verify charge rates reject &lt;= 0, pay rates reject &lt; 0, zero pay allowed.
/// **Validates: Requirements 6.12, 6.13**
/// </summary>
public class RateValidationTests
{
    private static Gen<decimal> PositiveDecimalGen =>
        Gen.Choose(1, 100000).Select(i => (decimal)i / 100m);

    private static Gen<decimal> NonNegativeDecimalGen =>
        Gen.Choose(0, 100000).Select(i => (decimal)i / 100m);

    private static Gen<decimal> NegativeDecimalGen =>
        Gen.Choose(-100000, -1).Select(i => (decimal)i / 100m);

    private static Gen<decimal> NonPositiveDecimalGen =>
        Gen.Choose(-100000, 0).Select(i => (decimal)i / 100m);

    /// <summary>
    /// Property 1: Charge rates strictly greater than zero are accepted by validation.
    /// **Validates: Requirements 6.12**
    /// </summary>
    [Property]
    public Property PositiveChargeRatesAreValid()
    {
        return Prop.ForAll(
            Arb.From(PositiveDecimalGen),
            Arb.From(NonNegativeDecimalGen),
            (charged, paid) =>
            {
                // Should not throw
                RateService.ValidateRate(charged, paid);
                return true;
            });
    }

    /// <summary>
    /// Property 2: Charge rates &lt;= 0 are rejected by validation.
    /// **Validates: Requirements 6.12**
    /// </summary>
    [Property]
    public Property NonPositiveChargeRatesAreInvalid()
    {
        return Prop.ForAll(
            Arb.From(NonPositiveDecimalGen),
            Arb.From(NonNegativeDecimalGen),
            (charged, paid) =>
            {
                try
                {
                    RateService.ValidateRate(charged, paid);
                    return false; // Should have thrown
                }
                catch (ArgumentException)
                {
                    return true;
                }
            });
    }

    /// <summary>
    /// Property 3: Pay rates &gt;= 0 are accepted by validation (zero explicitly allowed for unpaid workers).
    /// **Validates: Requirements 6.13**
    /// </summary>
    [Property]
    public Property NonNegativePayRatesAreValid()
    {
        return Prop.ForAll(
            Arb.From(PositiveDecimalGen),
            Arb.From(NonNegativeDecimalGen),
            (charged, paid) =>
            {
                // Should not throw
                RateService.ValidateRate(charged, paid);
                return true;
            });
    }

    /// <summary>
    /// Property 4: Negative pay rates are rejected by validation.
    /// **Validates: Requirements 6.13**
    /// </summary>
    [Property]
    public Property NegativePayRatesAreInvalid()
    {
        return Prop.ForAll(
            Arb.From(PositiveDecimalGen),
            Arb.From(NegativeDecimalGen),
            (charged, paid) =>
            {
                try
                {
                    RateService.ValidateRate(charged, paid);
                    return false; // Should have thrown
                }
                catch (ArgumentException)
                {
                    return true;
                }
            });
    }

    /// <summary>
    /// Property 5: Zero pay rate is explicitly allowed (for unpaid workers).
    /// **Validates: Requirements 6.13**
    /// </summary>
    [Property]
    public Property ZeroPayRateIsExplicitlyAllowed()
    {
        return Prop.ForAll(
            Arb.From(PositiveDecimalGen),
            charged =>
            {
                // Zero pay with any positive charge should not throw
                RateService.ValidateRate(charged, 0m);
                return true;
            });
    }
}
