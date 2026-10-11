using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TenveoPtz.Core.Updates;

namespace TenveoPtz.Mac.Infrastructure.Updates;

/// <summary>
/// Installs an update from a release disk image: mounts it, copies its app next to the running
/// one, then swaps the two bundles. macOS keeps running the old code from the renamed bundle
/// until the app restarts; the old bundle is deleted on a later start by <see cref="DeleteLeftovers"/>.
/// </summary>
internal sealed class DiskImageInstaller : IPackageInstaller
{
    private const string BackupSuffix = ".update-old";
    private const string StagingPrefix = ".TenveoPTZ-update-";

    private readonly string bundlePath;

    /// <param name="bundlePath">The running application bundle (<c>…/TenveoPTZ.app</c>).</param>
    public DiskImageInstaller(string bundlePath)
    {
        this.bundlePath = string.IsNullOrWhiteSpace(bundlePath) ? throw new ArgumentException("A bundle path is required.", nameof(bundlePath)) : bundlePath.TrimEnd('/');
    }

    private string Folder => Path.GetDirectoryName(bundlePath)!;

    private string BackupPath => bundlePath + BackupSuffix;

    public void Install(string packagePath)
    {
        if (bundlePath.StartsWith("/Volumes/", StringComparison.Ordinal) || bundlePath.Contains("/AppTranslocation/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Move Tenveo PTZ to the Applications folder first: it cannot update itself where it is.");
        }

        var staging = Path.Combine(Folder, StagingPrefix + Guid.NewGuid().ToString("N") + ".app");
        var mountPoint = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "TenveoPTZ-mount-" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            Run("/usr/bin/hdiutil", "attach", packagePath, "-nobrowse", "-readonly", "-noautoopen", "-mountpoint", mountPoint);
            try
            {
                var app = Directory.GetDirectories(mountPoint, "*.app").FirstOrDefault(IsApplication)
                    ?? throw new InvalidDataException("The downloaded update does not contain the application.");
                // ditto keeps permissions, extended attributes and the code signature intact.
                Run("/usr/bin/ditto", app, staging);
            }
            finally
            {
                Run("/usr/bin/hdiutil", "detach", mountPoint, "-force");
            }

            Swap(staging);
        }
        finally
        {
            TryDelete(staging);
            TryDelete(mountPoint);
        }
    }

    /// <summary>Deletes the bundle moved aside by a previous update and any interrupted download.</summary>
    public void DeleteLeftovers()
    {
        TryDelete(BackupPath);
        if (Directory.Exists(Folder))
        {
            foreach (var staging in Directory.GetDirectories(Folder, StagingPrefix + "*"))
            {
                TryDelete(staging);
            }
        }
    }

    private void Swap(string staging)
    {
        TryDelete(BackupPath);
        Directory.Move(bundlePath, BackupPath);
        try
        {
            Directory.Move(staging, bundlePath);
        }
        catch
        {
            Directory.Move(BackupPath, bundlePath);
            throw;
        }
    }

    private static bool IsApplication(string bundle) => File.Exists(Path.Combine(bundle, "Contents", "MacOS", "TenveoPTZ"));

    private static void Run(string program, params string[] arguments)
    {
        var start = new ProcessStartInfo(program) { RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new IOException($"Cannot run {program}.");
        process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new IOException($"{Path.GetFileName(program)} failed: {error.Trim()}");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            // A leftover folder is harmless; retried on the next start.
        }
    }
}
