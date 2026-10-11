using System;

namespace TenveoPtz.Core.Updates;

/// <summary>Operating system a release asset is built for; each release carries one per platform.</summary>
public enum ReleasePlatform
{
    /// <summary><c>TenveoPTZ-1.2.3.4.exe</c>, the stand-alone application (the <c>.msi</c> installs the same file).</summary>
    Windows,

    /// <summary><c>TenveoPTZ-1.2.3.4.dmg</c>, a disk image holding the universal (Intel and Apple silicon) app.</summary>
    MacOS,
}

/// <summary>Release asset naming per platform.</summary>
public static class ReleasePackages
{
    public static string ExtensionFor(ReleasePlatform platform) => platform == ReleasePlatform.MacOS ? ".dmg" : ".exe";

    public static bool IsPackageFor(ReleasePlatform platform, string? assetName) =>
        assetName?.EndsWith(ExtensionFor(platform), StringComparison.OrdinalIgnoreCase) == true;
}
