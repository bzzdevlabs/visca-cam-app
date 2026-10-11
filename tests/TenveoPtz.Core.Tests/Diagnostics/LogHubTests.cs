using System;
using System.Collections.Generic;
using System.Linq;
using TenveoPtz.Core.Diagnostics;

namespace TenveoPtz.Core.Tests.Diagnostics;

public sealed class LogHubTests
{
    [Fact]
    public void Write_DropsEntriesBelowMinimumLevel()
    {
        var hub = new LogHub(LogLevel.Info, 10);
        var log = new Logger(hub, "Test");

        log.Debug("hidden");
        log.Info("shown");

        Assert.Equal("shown", Assert.Single(hub.Recent()).Message);
        Assert.False(log.IsDebugEnabled);
    }

    [Fact]
    public void MinimumLevel_CanBeLoweredAtRunTime()
    {
        var hub = new LogHub(LogLevel.Info, 10);
        var log = new Logger(hub, "Test");

        hub.MinimumLevel = LogLevel.Debug;
        log.Debug("traffic");

        Assert.Equal(LogLevel.Debug, Assert.Single(hub.Recent()).Level);
    }

    [Fact]
    public void Recent_KeepsOnlyTheNewestEntries()
    {
        var hub = new LogHub(LogLevel.Debug, 3);
        var log = new Logger(hub, "Test");

        for (var i = 1; i <= 5; i++)
        {
            log.Info("entry " + i);
        }

        Assert.Equal(new[] { "entry 3", "entry 4", "entry 5" }, hub.Recent().Select(e => e.Message));
    }

    [Fact]
    public void Write_ForwardsToSinksAndNotifiesSubscribers()
    {
        var sink = new ListSink();
        var hub = new LogHub(LogLevel.Info, 10, sink);
        LogEntry? notified = null;
        hub.EntryWritten += (_, e) => notified = e.Entry;

        new Logger(hub, "VISCA").Warning("slow");

        Assert.Equal("VISCA", Assert.Single(sink.Entries).Source);
        Assert.Same(sink.Entries[0], notified);
    }

    [Fact]
    public void Write_SurvivesAThrowingSubscriber()
    {
        var hub = new LogHub(LogLevel.Info, 10);
        hub.EntryWritten += (_, _) => throw new InvalidOperationException("broken viewer");

        new Logger(hub, "Test").Info("still logged");

        Assert.Single(hub.Recent());
    }

    [Fact]
    public void Entry_FormatsAsOneLineWithExceptionDetails()
    {
        var entry = new LogEntry(new DateTime(2026, 10, 11, 9, 5, 3, 42), LogLevel.Error, "Main", "Connection failed", new InvalidOperationException("COM3 is busy"));

        var text = entry.ToString();

        Assert.StartsWith("2026-10-11 09:05:03.042 ERROR   [Main] Connection failed", text);
        Assert.Contains("COM3 is busy", text);
    }

    [Fact]
    public void NullLogger_RecordsNothing()
    {
        Assert.False(Logger.Null.IsDebugEnabled);
        Logger.Null.Error("ignored", new InvalidOperationException());
    }

    private sealed class ListSink : ILogSink
    {
        public List<LogEntry> Entries { get; } = new();

        public bool IsEnabled(LogLevel level) => true;

        public void Write(LogEntry entry) => Entries.Add(entry);
    }
}
