using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TenveoPtz.Core.Execution;

namespace TenveoPtz.Core.Tests.Execution;

public sealed class BackgroundCommandExecutorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void Dispose_DrainsQueuedCommandsInOrder()
    {
        var executed = new List<int>();
        var executor = new BackgroundCommandExecutor("test");
        for (var i = 0; i < 5; i++)
        {
            var value = i;
            executor.Post(() => executed.Add(value));
        }

        executor.Dispose();

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, executed);
    }

    [Fact]
    public void Post_ReplacesPendingCommandWithSameKey()
    {
        using var gate = new ManualResetEventSlim();
        var executed = new List<string>();
        var executor = new BackgroundCommandExecutor("test");
        executor.Post(() => gate.Wait(Timeout));

        executor.Post(() => executed.Add("move left"), "pan-tilt");
        executor.Post(() => executed.Add("zoom"), "zoom");
        executor.Post(() => executed.Add("stop"), "pan-tilt");
        gate.Set();
        executor.Dispose();

        Assert.Equal(new[] { "zoom", "stop" }, executed);
    }

    [Fact]
    public void Failures_AreReportedAndDoNotStopTheWorker()
    {
        var faults = new List<Exception>();
        var ranAfterFailure = false;
        var executor = new BackgroundCommandExecutor("test");
        executor.Faulted += (_, e) => faults.Add(e.GetException());

        executor.Post(() => throw new IOException("port closed"));
        executor.Post(() => ranAfterFailure = true);
        executor.Dispose();

        Assert.Equal("port closed", Assert.Single(faults).Message);
        Assert.True(ranAfterFailure);
    }

    [Fact]
    public void Post_AfterDisposeThrows()
    {
        var executor = new BackgroundCommandExecutor("test");
        executor.Dispose();

        Assert.Throws<ObjectDisposedException>(() => executor.Post(() => { }));
    }
}
