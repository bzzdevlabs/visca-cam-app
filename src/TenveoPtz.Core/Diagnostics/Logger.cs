using System;

namespace TenveoPtz.Core.Diagnostics;

/// <summary>Writes entries for one component (the <see cref="Source"/>) to a sink.</summary>
public sealed class Logger
{
    /// <summary>Logger that records nothing: the default when a component is created without one.</summary>
    public static readonly Logger Null = new(NullSink.Instance, string.Empty);

    private readonly ILogSink sink;

    public Logger(ILogSink sink, string source)
    {
        this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public string Source { get; }

    public bool IsDebugEnabled => sink.IsEnabled(LogLevel.Debug);

    /// <summary>Logger for another component writing to the same sink.</summary>
    public Logger For(string source) => new(sink, source);

    public void Debug(string message) => Write(LogLevel.Debug, message, null);

    public void Info(string message) => Write(LogLevel.Info, message, null);

    public void Warning(string message, Exception? exception = null) => Write(LogLevel.Warning, message, exception);

    public void Error(string message, Exception? exception = null) => Write(LogLevel.Error, message, exception);

    private void Write(LogLevel level, string message, Exception? exception)
    {
        if (sink.IsEnabled(level))
        {
            sink.Write(new LogEntry(DateTime.Now, level, Source, message, exception));
        }
    }

    private sealed class NullSink : ILogSink
    {
        public static readonly NullSink Instance = new();

        public bool IsEnabled(LogLevel level) => false;

        public void Write(LogEntry entry)
        {
        }
    }
}
