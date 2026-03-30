using AuntiesCleaners.Client.Services;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// A testable subclass that bypasses Assembly.GetEntryAssembly()
/// by accepting a raw version string directly.
/// </summary>
internal class TestableAppVersionService : AppVersionService
{
    public TestableAppVersionService(string? rawVersion) : base(rawVersion) { }
}

/// <summary>
/// Property-based tests for AppVersionService version resolution logic.
/// Feature: app-version-display
/// </summary>
public class AppVersionServiceTests
{
    private static readonly char[] PrintableChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-+_/".ToCharArray();

    private static readonly char[] WhitespaceChars = { ' ', '\t', '\n', '\r' };

    private static Gen<string> NonWhitespaceStringGen =>
        from chars in Gen.ListOf(Gen.Elements(PrintableChars))
        where chars.Count > 0
        select new string(chars.ToArray());

    private static Gen<string?> WhitespaceOrNullStringGen =>
        Gen.OneOf(
            Gen.Constant<string?>(null),
            Gen.Constant<string?>(""),
            from chars in Gen.ListOf(Gen.Elements(WhitespaceChars))
            where chars.Count > 0
            select (string?)new string(chars.ToArray()));

    /// <summary>
    /// Feature: app-version-display, Property 2: Service reads assembly informational version.
    /// For any non-whitespace string, AppVersionService.Version returns that exact string.
    /// **Validates: Requirements 1.3, 2.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ServiceReadsNonWhitespaceVersion()
    {
        return Prop.ForAll(
            Arb.From(NonWhitespaceStringGen),
            version =>
            {
                var service = new TestableAppVersionService(version);
                return (service.Version == version)
                    .Label($"Expected '{version}' but got '{service.Version}'");
            });
    }

    /// <summary>
    /// Feature: app-version-display, Property 3: Fallback to dev on missing or empty version.
    /// For any null, empty, or whitespace-only value, AppVersionService.Version returns "dev".
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FallbackToDevOnMissingOrEmptyVersion()
    {
        return Prop.ForAll(
            Arb.From(WhitespaceOrNullStringGen),
            input =>
            {
                var service = new TestableAppVersionService(input);
                return (service.Version == "dev")
                    .Label($"Expected 'dev' for input '{input ?? "null"}' but got '{service.Version}'");
            });
    }
}
