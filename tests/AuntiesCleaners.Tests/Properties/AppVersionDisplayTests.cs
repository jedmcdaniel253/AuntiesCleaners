using AuntiesCleaners.Client.Services;
using AuntiesCleaners.Client.Shared;
using Bunit;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using Xunit;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// Property-based tests for AppVersionDisplay component.
/// Feature: app-version-display
/// </summary>
public class AppVersionDisplayTests
{
    private static readonly char[] PrintableChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-+_/".ToCharArray();

    private static Gen<string> NonEmptyVersionStringGen =>
        from chars in Gen.ListOf(Gen.Elements(PrintableChars))
        where chars.Count > 0
        select new string(chars.ToArray());

    /// <summary>
    /// Feature: app-version-display, Property 1: Version display renders service version.
    /// For any non-empty version string returned by IAppVersionService,
    /// the AppVersionDisplay component's rendered output should contain that exact version string prefixed with "v".
    /// **Validates: Requirements 1.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RendersVersionFromService()
    {
        return Prop.ForAll(
            Arb.From(NonEmptyVersionStringGen),
            version =>
            {
                using var ctx = new BunitContext();
                ctx.Services.AddMudServices();

                var mockService = Substitute.For<IAppVersionService>();
                mockService.Version.Returns(version);
                ctx.Services.AddSingleton<IAppVersionService>(mockService);

                var cut = ctx.Render<AppVersionDisplay>();
                var markup = cut.Markup;

                return markup.Contains($"v{version}")
                    .Label($"Expected markup to contain 'v{version}' but got: {markup}");
            });
    }
}
