using System;

namespace TenveoPtz.Core.Execution;

/// <summary>Periodic timer raising <see cref="Tick"/> on the thread that owns the camera (the UI thread).</summary>
public interface ITicker : IDisposable
{
    event EventHandler? Tick;

    bool IsRunning { get; }

    void Start();

    void Stop();
}
