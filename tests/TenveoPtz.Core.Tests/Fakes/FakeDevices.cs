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

    public IReadOnlyList<VideoDevice> GetDevices() => new[] { new VideoDevice(0, "Tenveo USB Camera") };

    public IVideoSession Start(VideoDevice device, bool preview)
    {
        Started.Add(preview);
        return new FakeVideoSession(device);
    }

    public ITicker Create(TimeSpan interval) => new ManualTicker();

    public IReadOnlyList<string> GetPortNames() => new[] { "COM3" };

    public IByteTransport Open(string portName, int baudRate) => new FakeTransport();
}

internal sealed class FakeVideoSession : IVideoSession
{
    public FakeVideoSession(VideoDevice device) => Device = device;

    public VideoDevice Device { get; }

    public ICameraAxes? Axes { get; } = new FakeCameraAxes();

    public void Dispose()
    {
    }
}
