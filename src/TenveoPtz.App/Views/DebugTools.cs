using System;
using TenveoPtz.Core.Diagnostics;

namespace TenveoPtz.App.Views;

/// <summary>What the debug console needs from the rest of the app.</summary>
internal sealed class DebugTools
{
    public DebugTools(LogHub log, string dataFolder, string logFolder)
    {
        Log = log ?? throw new ArgumentNullException(nameof(log));
        DataFolder = dataFolder;
        LogFolder = logFolder;
        Logger = new Logger(log, "Debug");
    }

    public LogHub Log { get; }

    public Logger Logger { get; }

    public string DataFolder { get; }

    public string LogFolder { get; }
}
