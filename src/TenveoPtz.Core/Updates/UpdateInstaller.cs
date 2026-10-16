using System;
using System.IO;
using TenveoPtz.Core.Localization;

namespace TenveoPtz.Core.Updates;

/// <summary>
/// Replaces the Windows executable with a downloaded one. Windows lets a running executable be
/// renamed but not overwritten, so it is first moved aside as <c>.update-old</c>, then the new
/// one is copied in. The old file is deleted on a later start, by <see cref="DeleteLeftovers"/>.
/// A failure restores the original file.
/// </summary>
public sealed class UpdateInstaller : IPackageInstaller
{
    /// <summary>Distinctive suffix: the cleanup must never touch the user's own <c>.old</c> files.</summary>
    private const string BackupExtension = ".update-old";

    private readonly string installFolder;
    private readonly string executableName;

    /// <param name="installFolder">Folder holding the application.</param>
    /// <param name="executableName">File name of the application executable.</param>
    public UpdateInstaller(string installFolder, string executableName)
    {
        this.installFolder = string.IsNullOrWhiteSpace(installFolder) ? throw new ArgumentException("A folder is required.", nameof(installFolder)) : installFolder;
        this.executableName = string.IsNullOrWhiteSpace(executableName) ? throw new ArgumentException("A file name is required.", nameof(executableName)) : executableName;
    }

    public void Install(string packagePath)
    {
        if (!IsWindowsExecutable(packagePath))
        {
            throw new InvalidDataException(Strings.UpdateNotWindowsProgram);
        }

        var target = Path.Combine(installFolder, executableName);
        var backup = target + BackupExtension;
        if (File.Exists(backup))
        {
            File.Delete(backup);
        }

        var movedAside = File.Exists(target);
        if (movedAside)
        {
            File.Move(target, backup);
        }

        try
        {
            File.Copy(packagePath, target);
        }
        catch
        {
            if (movedAside)
            {
                TryDeleteFile(target);
                File.Move(backup, target);
            }

            throw;
        }
    }

    /// <summary>Deletes the file moved aside by a previous update (best effort: it may still be in use).</summary>
    public void DeleteLeftovers()
    {
        if (!Directory.Exists(installFolder))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(installFolder, "*" + BackupExtension, SearchOption.TopDirectoryOnly))
        {
            TryDeleteFile(file);
        }
    }

    /// <summary>Portable executables start with the "MZ" DOS header.</summary>
    private static bool IsWindowsExecutable(string path)
    {
        using var stream = File.OpenRead(path);
        return stream.ReadByte() == 'M' && stream.ReadByte() == 'Z';
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            // Still in use (the previous process is exiting): retried on the next start.
        }
    }
}
