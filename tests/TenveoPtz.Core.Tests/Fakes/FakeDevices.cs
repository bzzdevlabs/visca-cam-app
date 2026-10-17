using System;
using System.Collections.Generic;
using TenveoPtz.Core.Execution;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Transport;
using TenveoPtz.Core.Video;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>Device discovery returning fixed lists; started video sessions are recorded.</summary>
internal sealed class FakeDevices : IVideoService, ISerialPortProvider, ITickerFactory
{
    /// <summary>The <c>preview</c> argument of each <see cref="Start"/> call.</summary>
    public List<bool> Started { get; } = new();

    /// <summary>Thrown when listing the devices, e.g. a broken driver.</summary>
    public Exception? FailWith { get; set; }

    public IReadOnlyList<VideoDevice> GetDevices() => FailWith is null ? new[] { new VideoDevice(0, "Tenveo USB Camera") } : throw FailWith;

    public IVideoSession Start(VideoDevice device, bool preview)
    {
        Started.Add(preview);
        return LastVideo = new FakeVideoSession(device);
    }

    public ITicker Create(TimeSpan interval) => new ManualTicker();

    public IReadOnlyList<string> GetPortNames() => new[] { "COM3" };

    /// <summary>The transport returned by the latest <see cref="Open"/>.</summary>
    public FakeTransport? LastTransport { get; private set; }

    /// <summary>The session returned by the latest <see cref="Start"/>.</summary>
    public FakeVideoSession? LastVideo { get; private set; }

    public IByteTransport Open(string portName, int baudRate) => LastTransport = new FakeTransport();
}

internal sealed class FakeVideoSession : IVideoSession
{
    public FakeVideoSession(VideoDevice device) => Device = device;

    public VideoDevice Device { get; }

    public ICameraAxes? Axes { get; } = new FakeCameraAxes();

    public bool Disposed { get; private set; }

    public void Dispose() => Disposed = true;
}
