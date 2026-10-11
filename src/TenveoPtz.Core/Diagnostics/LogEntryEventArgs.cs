using System;

namespace TenveoPtz.Core.Diagnostics;

public sealed class LogEntryEventArgs : EventArgs
{
    public LogEntryEventArgs(LogEntry entry)
    {
        Entry = entry;
    }

    public LogEntry Entry { get; }
}
