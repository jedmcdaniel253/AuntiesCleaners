using Microsoft.AspNetCore.Components;

namespace AuntiesCleaners.Client.Services;

/// <summary>
/// Builds absolute URIs from relative routes using NavigationManager.BaseUri.
/// Works correctly on both localhost (base="/") and GitHub Pages (base="/AuntiesCleaners/").
/// </summary>
public static class NavigationHelper
{
    public static void NavigateToRoute(this NavigationManager nav, string relativeRoute)
    {
        var route = relativeRoute.TrimStart('/');
        nav.NavigateTo(nav.BaseUri + route);
    }
}
