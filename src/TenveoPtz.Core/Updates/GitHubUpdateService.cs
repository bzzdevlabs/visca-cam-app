using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TenveoPtz.Core.Updates;

/// <summary>
/// Update source backed by the GitHub releases of a repository. The latest release's tag
/// (<c>v1.2.3.45</c>) gives its version and its <c>.zip</c> asset holds the application files.
/// </summary>
public sealed class GitHubUpdateService : IUpdateService
{
    private readonly HttpClient http;
    private readonly string repository;
    private readonly Version currentVersion;
    private readonly UpdateInstaller installer;

    /// <param name="http">Client used for the GitHub API and the download.</param>
    /// <param name="repository">Repository as <c>owner/name</c>.</param>
    /// <param name="currentVersion">Version of the running application.</param>
    /// <param name="installer">Replaces the application files with the downloaded ones.</param>
    public GitHubUpdateService(HttpClient http, string repository, Version currentVersion, UpdateInstaller installer)
    {
        this.http = http ?? throw new ArgumentNullException(nameof(http));
        this.repository = string.IsNullOrWhiteSpace(repository) ? throw new ArgumentException("A repository is required.", nameof(repository)) : repository;
        this.currentVersion = Normalize(currentVersion ?? throw new ArgumentNullException(nameof(currentVersion)));
        this.installer = installer ?? throw new ArgumentNullException(nameof(installer));
    }

    public async Task<ReleaseInfo?> FindUpdateAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repository}/releases/latest");
        request.Headers.UserAgent.ParseAdd("TenveoPTZ/" + currentVersion); // GitHub rejects requests without one.
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null; // Nothing released yet.
        }

        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var release = (GitHubRelease?)new DataContractJsonSerializer(typeof(GitHubRelease)).ReadObject(stream);
        return ToUpdate(release);
    }

    public async Task InstallAsync(ReleaseInfo release, CancellationToken cancellationToken = default)
    {
        if (release is null)
        {
            throw new ArgumentNullException(nameof(release));
        }

        var archive = Path.Combine(Path.GetTempPath(), $"TenveoPTZ-{release.Version}-{Guid.NewGuid():N}.zip");
        try
        {
            using (var response = await http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var target = File.Create(archive);
                await source.CopyToAsync(target, 81920, cancellationToken).ConfigureAwait(false);
            }

            await Task.Run(() => installer.Install(archive), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            TryDelete(archive);
        }
    }

    /// <summary>Parses a release tag such as <c>v0.2.0.41</c>; missing parts count as 0.</summary>
    public static Version? ParseTag(string? tag)
    {
        var text = tag?.Trim().TrimStart('v', 'V');
        return Version.TryParse(text, out var version) ? Normalize(version) : null;
    }

    private ReleaseInfo? ToUpdate(GitHubRelease? release)
    {
        if (release is null || release.Draft || release.Prerelease)
        {
            return null;
        }

        var version = ParseTag(release.TagName);
        var asset = release.Assets?.FirstOrDefault(a => a.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);
        if (version is null || version <= currentVersion
            || !Uri.TryCreate(asset?.DownloadUrl, UriKind.Absolute, out var download)
            || !Uri.TryCreate(release.PageUrl, UriKind.Absolute, out var page))
        {
            return null;
        }

        return new ReleaseInfo(version, download, page);
    }

    /// <summary>1.2 and 1.2.0.0 must compare equal: <see cref="Version"/> treats missing parts as -1.</summary>
    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            // A leftover file in the temp folder is harmless.
        }
    }

    [DataContract]
    private sealed class GitHubRelease
    {
        [DataMember(Name = "tag_name")]
        public string? TagName { get; set; }

        [DataMember(Name = "html_url")]
        public string? PageUrl { get; set; }

        [DataMember(Name = "draft")]
        public bool Draft { get; set; }

        [DataMember(Name = "prerelease")]
        public bool Prerelease { get; set; }

        [DataMember(Name = "assets")]
        public GitHubAsset[]? Assets { get; set; }
    }

    [DataContract]
    private sealed class GitHubAsset
    {
        [DataMember(Name = "name")]
        public string? Name { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string? DownloadUrl { get; set; }
    }
}
