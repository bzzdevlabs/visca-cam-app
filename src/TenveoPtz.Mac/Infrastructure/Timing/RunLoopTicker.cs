using System;
using Foundation;
using TenveoPtz.Core.Execution;

namespace TenveoPtz.Mac.Infrastructure.Timing;

/// <summary>
/// Ticker on the main run loop, in the "common" modes so it keeps ticking while a button is held
/// down (AppKit then runs the loop in event-tracking mode).
/// </summary>
internal sealed class RunLoopTicker : ITicker
{
    private readonly TimeSpan interval;
    private NSTimer? timer;

    public RunLoopTicker(TimeSpan interval)
    {
        this.interval = interval;
    }

    public event EventHandler? Tick;

    public bool IsRunning => timer is not null;

    public void Start()
    {
        if (timer is not null)
        {
            return;
        }

        timer = NSTimer.CreateRepeatingTimer(interval, _ => Tick?.Invoke(this, EventArgs.Empty));
        NSRunLoop.Main.AddTimer(timer, NSRunLoopMode.Common);
    }

    public void Stop()
    {
        timer?.Invalidate();
        timer?.Dispose();
        timer = null;
    }

    public void Dispose() => Stop();
}
