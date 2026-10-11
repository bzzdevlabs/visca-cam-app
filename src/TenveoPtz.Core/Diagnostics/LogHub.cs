using System;
using System.Collections.Generic;

namespace TenveoPtz.Core.Diagnostics;

/// <summary>
/// Application-wide sink: filters entries by <see cref="MinimumLevel"/>, keeps the most recent
/// ones in memory for the debug console and forwards them to the other sinks (e.g. the log file).
/// </summary>
public sealed class LogHub : ILogSink
{
    private readonly object gate = new();
    private readonly Queue<LogEntry> recent = new();
    private readonly IReadOnlyList<ILogSink> sinks;
    private readonly int capacity;
    private volatile LogLevel minimumLevel;

    public LogHub(LogLevel minimumLevel, int capacity, params ILogSink[] sinks)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "The capacity must be positive.");
        }

        this.minimumLevel = minimumLevel;
        this.capacity = capacity;
        this.sinks = sinks ?? Array.Empty<ILogSink>();
    }

    /// <summary>Raised after an entry is recorded, on the thread that wrote it.</summary>
    public event EventHandler<LogEntryEventArgs>? EntryWritten;

    public LogLevel MinimumLevel
    {
        get => minimumLevel;
        set => minimumLevel = value;
    }

    public bool IsEnabled(LogLevel level) => level >= minimumLevel;

    /// <summary>Snapshot of the most recent entries, oldest first.</summary>
    public IReadOnlyList<LogEntry> Recent()
    {
        lock (gate)
        {
            return recent.ToArray();
        }
    }

    public void Write(LogEntry entry)
    {
        if (entry is null || !IsEnabled(entry.Level))
        {
            return;
        }

        lock (gate)
        {
            if (recent.Count == capacity)
            {
                recent.Dequeue();
            }

            recent.Enqueue(entry);
        }

        foreach (var sink in sinks)
        {
            sink.Write(entry);
        }

        try
        {
            EntryWritten?.Invoke(this, new LogEntryEventArgs(entry));
        }
        catch (Exception)
        {
            // Logging must never break the caller, whatever a subscriber does.
        }
    }
}
