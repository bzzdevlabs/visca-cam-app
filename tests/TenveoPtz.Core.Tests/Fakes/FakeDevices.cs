using System;
using System.Collections.Generic;
using TenveoPtz.Core.Transport;
using TenveoPtz.Core.Video;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>Device discovery returning fixed lists.</summary>
internal sealed class FakeDevices : IVideoService, ISerialPortProvider
{
    public IReadOnlyList<VideoDevice> GetDevices() => new[] { new VideoDevice(0, "Tenveo USB Camera") };

    public IVideoSession Start(VideoDevice device) => throw new NotSupportedException();

    public IReadOnlyList<string> GetPortNames() => new[] { "COM3" };

    public IByteTransport Open(string portName, int baudRate) => new FakeTransport();
}
