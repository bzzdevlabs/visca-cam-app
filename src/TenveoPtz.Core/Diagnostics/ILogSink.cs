namespace TenveoPtz.Core.Diagnostics;

/// <summary>Destination of log entries. Implementations must be thread-safe and must never throw.</summary>
public interface ILogSink
{
    /// <summary>Whether entries of <paramref name="level"/> are recorded; lets callers skip costly formatting.</summary>
    bool IsEnabled(LogLevel level);

    void Write(LogEntry entry);
}
