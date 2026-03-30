using System.Reflection;

namespace AuntiesCleaners.Client.Services;

public class AppVersionService : IAppVersionService
{
    public string Version { get; }

    public AppVersionService()
    {
        var attr = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Version = ResolveVersion(attr?.InformationalVersion);
    }

    /// <summary>
    /// Constructor for testing: accepts a raw version string directly.
    /// </summary>
    protected AppVersionService(string? rawVersion)
    {
        Version = ResolveVersion(rawVersion);
    }

    private static string ResolveVersion(string? rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
            return "dev";

        // Strip the "+commithash" suffix appended by the .NET SDK
        var plusIndex = rawVersion.IndexOf('+');
        return plusIndex > 0 ? rawVersion[..plusIndex] : rawVersion;
    }
}
