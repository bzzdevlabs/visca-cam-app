using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TenveoPtz.Core.Updates;

namespace TenveoPtz.Core.Tests.Updates;

public sealed class GitHubUpdateServiceTests : IDisposable
{
    private const string LatestUrl = "https://api.github.com/repos/owner/repo/releases/latest";
    private const string ExeUrl = "https://github.com/owner/repo/releases/download/v0.3.0.50/TenveoPTZ-0.3.0.50.exe";
    private const string DmgUrl = "https://github.com/owner/repo/releases/download/v0.3.0.50/TenveoPTZ-0.3.0.50.dmg";

    private readonly FakeHandler handler = new();
    private readonly HttpClient http;
    private readonly string app = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "TenveoPtzTests-" + Guid.NewGuid().ToString("N"))).FullName;

    public GitHubUpdateServiceTests() => http = new HttpClient(handler);

    public void Dispose()
    {
        http.Dispose();
        Directory.Delete(app, recursive: true);
    }

    [Fact]
    public async Task FindUpdate_ReturnsNewerRelease()
    {
        handler.Json(LatestUrl, Release("v0.3.0.50"));

        var update = await Service("0.2.0.41").FindUpdateAsync();

        Assert.NotNull(update);
        Assert.Equal(new Version(0, 3, 0, 50), update!.Version);
        Assert.Equal(new Uri(ExeUrl), update.DownloadUrl);
        Assert.Equal(new Uri("https://github.com/owner/repo/releases/tag/v0.3.0.50"), update.PageUrl);
        Assert.Contains("TenveoPTZ", handler.Requests[0].Headers.UserAgent.ToString());
    }

    [Theory]
    [InlineData("v0.2.0.41")] // Same version.
    [InlineData("v0.2.0")] // Older (0.2.0.0).
    [InlineData("nightly")] // Not a version.
    public async Task FindUpdate_IgnoresReleasesThatAreNotNewer(string tag)
    {
        handler.Json(LatestUrl, Release(tag));

        Assert.Null(await Service("0.2.0.41").FindUpdateAsync());
    }

    [Fact]
    public async Task FindUpdate_ComparesShortVersionsAsPadded()
    {
        handler.Json(LatestUrl, Release("v1.2"));

        Assert.Null(await Service("1.2.0.0").FindUpdateAsync());
    }

    [Fact]
    public async Task FindUpdate_IgnoresReleaseWithoutExecutable()
    {
        handler.Json(LatestUrl, Release("v0.3.0.50").Replace("50.exe", "50.zip"));

        Assert.Null(await Service("0.2.0.41").FindUpdateAsync());
    }

    [Fact]
    public async Task FindUpdate_PicksTheArchiveOfItsPlatform()
    {
        handler.Json(LatestUrl, Release("v0.3.0.50"));

        Assert.Equal(new Uri(ExeUrl), (await Service("0.2.0.41").FindUpdateAsync())!.DownloadUrl);
        Assert.Equal(new Uri(DmgUrl), (await Service("0.2.0.41", ReleasePlatform.MacOS).FindUpdateAsync())!.DownloadUrl);
    }

    [Fact]
    public async Task FindUpdate_IgnoresReleaseWithoutArchiveForThePlatform()
    {
        handler.Json(LatestUrl, Release("v0.3.0.50").Replace(".dmg", ".pkg"));

        Assert.Null(await Service("0.2.0.41", ReleasePlatform.MacOS).FindUpdateAsync());
    }

    [Fact]
    public async Task FindUpdate_NoReleaseYet()
    {
        handler.Responses[LatestUrl] = () => new HttpResponseMessage(HttpStatusCode.NotFound);

        Assert.Null(await Service("0.2.0.41").FindUpdateAsync());
    }

    [Fact]
    public async Task FindUpdate_ServerErrorThrows()
    {
        handler.Responses[LatestUrl] = () => new HttpResponseMessage(HttpStatusCode.Forbidden); // Rate limited.

        await Assert.ThrowsAsync<HttpRequestException>(() => Service("0.2.0.41").FindUpdateAsync());
    }

    [Fact]
    public async Task Install_DownloadsAndInstallsTheArchive()
    {
        File.WriteAllText(Path.Combine(app, "TenveoPTZ.exe"), "old");
        handler.Json(LatestUrl, Release("v0.3.0.50"));
        handler.Responses[ExeUrl] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.ASCII.GetBytes("MZ new")) };
        var service = Service("0.2.0.41");

        await service.InstallAsync((await service.FindUpdateAsync())!);

        Assert.Equal("MZ new", File.ReadAllText(Path.Combine(app, "TenveoPTZ.exe")));
    }

    [Theory]
    [InlineData("v1.2.3.4", 1, 2, 3, 4)]
    [InlineData("1.2.3", 1, 2, 3, 0)]
    [InlineData(" V2.0 ", 2, 0, 0, 0)]
    public void ParseTag_ReadsVersions(string tag, int major, int minor, int build, int revision) =>
        Assert.Equal(new Version(major, minor, build, revision), GitHubUpdateService.ParseTag(tag));

    private GitHubUpdateService Service(string version, ReleasePlatform platform = ReleasePlatform.Windows) =>
        new(http, "owner/repo", Version.Parse(version), new UpdateInstaller(app, "TenveoPTZ.exe"), platform);

    private static string Release(string tag) => $$"""
        {
          "tag_name": "{{tag}}",
          "html_url": "https://github.com/owner/repo/releases/tag/{{tag}}",
          "draft": false,
          "prerelease": false,
          "body": "## What's Changed",
          "assets": [
            { "name": "TenveoPTZ-0.3.0.50.dmg", "size": 9481936, "browser_download_url": "{{DmgUrl}}" },
            { "name": "TenveoPTZ-0.3.0.50.msi", "size": 245760, "browser_download_url": "https://github.com/owner/repo/releases/download/v0.3.0.50/TenveoPTZ-0.3.0.50.msi" },
            { "name": "TenveoPTZ-0.3.0.50.exe", "size": 81936, "browser_download_url": "{{ExeUrl}}" }
          ]
        }
        """;

    private sealed class FakeHandler : HttpMessageHandler
    {
        public Dictionary<string, Func<HttpResponseMessage>> Responses { get; } = new();

        public List<HttpRequestMessage> Requests { get; } = new();

        public void Json(string url, string json) =>
            Responses[url] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(Responses.TryGetValue(request.RequestUri!.ToString(), out var respond)
                ? respond()
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
