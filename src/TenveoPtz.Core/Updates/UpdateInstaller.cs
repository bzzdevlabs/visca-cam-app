using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TenveoPtz.Core.Updates;

/// <summary>
/// Replaces the application files with those of a release archive. Windows lets a running
/// executable (or a loaded DLL) be renamed but not overwritten, so each file is first moved
/// aside as <c>.update-old</c>, then the new one is copied in. Those files are deleted on a
/// later start, by <see cref="DeleteLeftovers"/>. A failure restores the original files.
/// Release archives are flat: only their top-level files are installed.
/// </summary>
public sealed class UpdateInstaller
{
    /// <summary>Distinctive suffix: the cleanup must never touch the user's own <c>.old</c> files.</summary>
    private const string BackupExtension = ".update-old";

    private readonly string installFolder;
    private readonly string requiredFile;

    /// <param name="installFolder">Folder holding the application files.</param>
    /// <param name="requiredFile">File the archive must contain (the executable), as a sanity check.</param>
    public UpdateInstaller(string installFolder, string requiredFile)
    {
        this.installFolder = string.IsNullOrWhiteSpace(installFolder) ? throw new ArgumentException("A folder is required.", nameof(installFolder)) : installFolder;
        this.requiredFile = string.IsNullOrWhiteSpace(requiredFile) ? throw new ArgumentException("A file name is required.", nameof(requiredFile)) : requiredFile;
    }

    public void Install(string archivePath)
    {
        var staging = Path.Combine(Path.GetTempPath(), "TenveoPTZ-update-" + Guid.NewGuid().ToString("N"));
        try
        {
            ZipFile.ExtractToDirectory(archivePath, staging); // Rejects entries escaping the folder.
            if (!File.Exists(Path.Combine(staging, requiredFile)))
            {
                throw new InvalidDataException($"The downloaded update does not contain {requiredFile}.");
            }

            ReplaceFiles(staging);
        }
        finally
        {
            TryDeleteFolder(staging);
        }
    }

    /// <summary>Deletes the files moved aside by a previous update (best effort: they may still be in use).</summary>
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

    private void ReplaceFiles(string sourceFolder)
    {
        var replaced = new List<(string Target, string? Backup)>();
        try
        {
            foreach (var source in Directory.GetFiles(sourceFolder).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var target = Path.Combine(installFolder, Path.GetFileName(source));

                string? backup = null;
                if (File.Exists(target))
                {
                    backup = target + BackupExtension;
                    if (File.Exists(backup))
                    {
                        File.Delete(backup);
                    }

                    File.Move(target, backup);
                }

                replaced.Add((target, backup));
                File.Copy(source, target);
            }
        }
        catch
        {
            Restore(replaced);
            throw;
        }
    }

    private static void Restore(List<(string Target, string? Backup)> replaced)
    {
        for (var i = replaced.Count - 1; i >= 0; i--)
        {
            var (target, backup) = replaced[i];
            try
            {
                if (File.Exists(target))
                {
                    File.Delete(target);
                }

                if (backup is not null)
                {
                    File.Move(backup, target);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Keep restoring the other files.
            }
        }
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

    private static void TryDeleteFolder(string path)
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
            // A leftover folder in the temp folder is harmless.
        }
    }
}
