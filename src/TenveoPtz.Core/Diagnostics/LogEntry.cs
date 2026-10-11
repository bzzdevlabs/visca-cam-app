using System;
using System.Globalization;
using System.Text;

namespace TenveoPtz.Core.Diagnostics;

/// <summary>One immutable log record.</summary>
public sealed class LogEntry
{
    public LogEntry(DateTime timestamp, LogLevel level, string source, string message, Exception? exception = null)
    {
        Timestamp = timestamp;
        Level = level;
        Source = source ?? string.Empty;
        Message = message ?? string.Empty;
        Exception = exception;
    }

    public DateTime Timestamp { get; }

    public LogLevel Level { get; }

    /// <summary>Component that wrote the entry, e.g. "VISCA".</summary>
    public string Source { get; }

    public string Message { get; }

    public Exception? Exception { get; }

    /// <summary>Formats the entry as one text line, followed by the exception details when there is one.</summary>
    public override string ToString()
    {
        var text = new StringBuilder()
            .Append(Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
            .Append(' ')
            .Append(Level.ToString().ToUpperInvariant().PadRight(7))
            .Append(" [").Append(Source).Append("] ")
            .Append(Message);
        if (Exception is not null)
        {
            text.Append(Environment.NewLine).Append(Exception);
        }

        return text.ToString();
    }
}
