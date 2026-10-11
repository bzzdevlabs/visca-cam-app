using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace TenveoPtz.Core.Diagnostics;

/// <summary>
/// Appends entries to one text file per day (<c>tenveo-yyyyMMdd.log</c>) and deletes the files
/// older than <see cref="RetainedDays"/>. I/O failures are swallowed: logging must never break the app.
/// </summary>
public sealed class FileLogSink : ILogSink
{
    public const int RetainedDays = 7;

    private const string FilePrefix = "tenveo-";
    private const string FileExtension = ".log";

    private readonly object gate = new();
    private readonly Func<DateTime> today;

    public FileLogSink(string folder)
        : this(folder, () => DateTime.Today)
    {
    }

    internal FileLogSink(string folder, Func<DateTime> today)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            throw new ArgumentException("A folder is required.", nameof(folder));
        }

        Folder = folder;
        this.today = today;
        DeleteExpiredFiles();
    }

    public string Folder { get; }

    /// <summary>File written today.</summary>
    public string CurrentFile => PathFor(today());

    public bool IsEnabled(LogLevel level) => true;

    public void Write(LogEntry entry)
    {
        if (entry is null)
        {
            return;
        }

        lock (gate)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.AppendAllText(PathFor(entry.Timestamp.Date), entry + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Best effort: the entry is still kept in memory by the hub.
            }
        }
    }

    private string PathFor(DateTime day) =>
        Path.Combine(Folder, FilePrefix + day.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + FileExtension);

    private void DeleteExpiredFiles()
    {
        try
        {
            if (!Directory.Exists(Folder))
            {
                return;
            }

            var oldestKept = PathFor(today().AddDays(1 - RetainedDays));
            var expired = Directory.GetFiles(Folder, FilePrefix + "*" + FileExtension)
                .Where(file => string.CompareOrdinal(file, oldestKept) < 0);
            foreach (var file in expired)
            {
                File.Delete(file);
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            // Old files are only a disk-space concern.
        }
    }
}
