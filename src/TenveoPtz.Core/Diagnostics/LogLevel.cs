namespace TenveoPtz.Core.Diagnostics;

/// <summary>Severity of a log entry, from the most verbose to the most severe.</summary>
public enum LogLevel
{
    /// <summary>Protocol traffic and user intents; only recorded when verbose logging is on.</summary>
    Debug,
    Info,
    Warning,
    Error,
}
