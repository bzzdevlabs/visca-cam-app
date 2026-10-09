using System;
using TenveoPtz.Core.Execution;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>Ticker fired explicitly by the test.</summary>
internal sealed class ManualTicker : ITicker
{
    public event EventHandler? Tick;

    public bool IsRunning { get; private set; }

    public bool Disposed { get; private set; }

    public void Start() => IsRunning = true;

    public void Stop() => IsRunning = false;

    public void Fire() => Tick?.Invoke(this, EventArgs.Empty);

    public void Dispose() => Disposed = true;
}
