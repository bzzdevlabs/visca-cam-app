using System;

namespace TenveoPtz.Core.Updates;

/// <summary>A published version of the application that can be installed.</summary>
public sealed class ReleaseInfo
{
    public ReleaseInfo(Version version, Uri downloadUrl, Uri pageUrl)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        DownloadUrl = downloadUrl ?? throw new ArgumentNullException(nameof(downloadUrl));
        PageUrl = pageUrl ?? throw new ArgumentNullException(nameof(pageUrl));
    }

    public Version Version { get; }

    /// <summary>The zip archive holding the application files.</summary>
    public Uri DownloadUrl { get; }

    /// <summary>Human-readable release page (release notes).</summary>
    public Uri PageUrl { get; }
}
