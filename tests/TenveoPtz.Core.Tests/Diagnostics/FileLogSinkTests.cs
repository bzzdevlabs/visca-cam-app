using System;
using System.IO;
using TenveoPtz.Core.Diagnostics;

namespace TenveoPtz.Core.Tests.Diagnostics;

public sealed class FileLogSinkTests : IDisposable
{
    private static readonly DateTime Today = new(2026, 10, 11);

    private readonly string folder = Path.Combine(Path.GetTempPath(), "TenveoPtzTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Write_AppendsToTheFileOfTheDay()
    {
        var sink = new FileLogSink(folder, () => Today);

        sink.Write(new LogEntry(Today.AddHours(9), LogLevel.Info, "App", "first"));
        sink.Write(new LogEntry(Today.AddHours(10), LogLevel.Info, "App", "second"));

        Assert.Equal(Path.Combine(folder, "tenveo-20261011.log"), sink.CurrentFile);
        var lines = File.ReadAllLines(sink.CurrentFile);
        Assert.Equal(2, lines.Length);
        Assert.EndsWith("first", lines[0]);
        Assert.EndsWith("second", lines[1]);
    }

    [Fact]
    public void Constructor_DeletesFilesOlderThanTheRetention()
    {
        Directory.CreateDirectory(folder);
        var expired = Path.Combine(folder, "tenveo-20261004.log");
        var kept = Path.Combine(folder, "tenveo-20261005.log");
        var unrelated = Path.Combine(folder, "notes.txt");
        File.WriteAllText(expired, "old");
        File.WriteAllText(kept, "recent");
        File.WriteAllText(unrelated, "keep me");

        _ = new FileLogSink(folder, () => Today);

        Assert.False(File.Exists(expired));
        Assert.True(File.Exists(kept));
        Assert.True(File.Exists(unrelated));
    }
}
