using System;
using System.Windows.Forms;
using TenveoPtz.Core.Execution;

namespace TenveoPtz.App.Infrastructure.Timing;

/// <summary>Ticker raising events on the UI thread, where the DirectShow objects live.</summary>
internal sealed class WinFormsTicker : ITicker
{
    private readonly Timer timer;

    public WinFormsTicker(TimeSpan interval)
    {
        timer = new Timer { Interval = Math.Max(1, (int)interval.TotalMilliseconds) };
        timer.Tick += (sender, e) => Tick?.Invoke(this, e);
    }

    public event EventHandler? Tick;

    public bool IsRunning => timer.Enabled;

    public void Start() => timer.Start();

    public void Stop() => timer.Stop();

    public void Dispose() => timer.Dispose();
}
