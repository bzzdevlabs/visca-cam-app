using System.Threading;
using System.Threading.Tasks;

namespace TenveoPtz.Core.Updates;

/// <summary>Finds newer releases of the application and installs them.</summary>
public interface IUpdateService
{
    /// <summary>Returns the latest release when it is newer than the running version, otherwise <c>null</c>.</summary>
    Task<ReleaseInfo?> FindUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads <paramref name="release"/> and replaces the application. The running
    /// version keeps working until it exits; the next start runs the new version.
    /// </summary>
    Task InstallAsync(ReleaseInfo release, CancellationToken cancellationToken = default);
}
