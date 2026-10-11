using System;
using System.Threading;
using System.Threading.Tasks;
using TenveoPtz.Core.Updates;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>Update source answering synchronously, so presenter tests need no waiting.</summary>
internal sealed class FakeUpdateService : IUpdateService
{
    public ReleaseInfo? Available { get; set; }

    public Exception? CheckFailure { get; set; }

    public Exception? InstallFailure { get; set; }

    public int CheckCount { get; private set; }

    public ReleaseInfo? Installed { get; private set; }

    public Task<ReleaseInfo?> FindUpdateAsync(CancellationToken cancellationToken = default)
    {
        CheckCount++;
        return CheckFailure is null ? Task.FromResult(Available) : Task.FromException<ReleaseInfo?>(CheckFailure);
    }

    public Task InstallAsync(ReleaseInfo release, CancellationToken cancellationToken = default)
    {
        if (InstallFailure is not null)
        {
            return Task.FromException(InstallFailure);
        }

        Installed = release;
        return Task.CompletedTask;
    }

    public static ReleaseInfo Release(string version) => new(
        Version.Parse(version),
        new Uri($"https://example.test/TenveoPTZ-{version}.zip"),
        new Uri($"https://example.test/releases/v{version}"));
}
