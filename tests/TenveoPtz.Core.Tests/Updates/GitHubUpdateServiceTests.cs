using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TenveoPtz.Core.Updates;

namespace TenveoPtz.Core.Tests.Updates;

public sealed class GitHubUpdateServiceTests : IDisposable
{
    private const string LatestUrl = "https://api.github.com/repos/owner/repo/releases/latest";
    private const string ZipUrl = "https://github.com/owner/repo/releases/download/v0.3.0.50/TenveoPTZ-0.3.0.50.zip";

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
        Assert.Equal(new Uri(ZipUrl), update.DownloadUrl);
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
    public async Task FindUpdate_IgnoresReleaseWithoutZip()
    {
        handler.Json(LatestUrl, Release("v0.3.0.50").Replace(".zip", ".tar.gz"));

        Assert.Null(await Service("0.2.0.41").FindUpdateAsync());
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
        handler.Responses[ZipUrl] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Archive("TenveoPTZ.exe", "new")) };
        var service = Service("0.2.0.41");

        await service.InstallAsync((await service.FindUpdateAsync())!);

        Assert.Equal("new", File.ReadAllText(Path.Combine(app, "TenveoPTZ.exe")));
    }

    [Theory]
    [InlineData("v1.2.3.4", 1, 2, 3, 4)]
    [InlineData("1.2.3", 1, 2, 3, 0)]
    [InlineData(" V2.0 ", 2, 0, 0, 0)]
    public void ParseTag_ReadsVersions(string tag, int major, int minor, int build, int revision) =>
        Assert.Equal(new Version(major, minor, build, revision), GitHubUpdateService.ParseTag(tag));

    private GitHubUpdateService Service(string version) =>
        new(http, "owner/repo", Version.Parse(version), new UpdateInstaller(app, "TenveoPTZ.exe"));

    private static string Release(string tag) => $$"""
        {
          "tag_name": "{{tag}}",
          "html_url": "https://github.com/owner/repo/releases/tag/{{tag}}",
          "draft": false,
          "prerelease": false,
          "body": "## What's Changed",
          "assets": [
            { "name": "TenveoPTZ-0.3.0.50.zip", "size": 81936, "browser_download_url": "{{ZipUrl}}" }
          ]
        }
        """;

    private static byte[] Archive(string name, string content)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        using (var writer = new StreamWriter(zip.CreateEntry(name).Open()))
        {
            writer.Write(content);
        }

        return buffer.ToArray();
    }

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
