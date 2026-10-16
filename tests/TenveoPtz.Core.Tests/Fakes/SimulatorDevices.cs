using System;
using System.Collections.Generic;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Simulation;
using TenveoPtz.Core.Transport;
using TenveoPtz.Core.Video;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>Device discovery offering one simulated UVC camera, driven by a <see cref="SimulatedClock"/>.</summary>
internal sealed class SimulatorDevices : IVideoService, ISerialPortProvider
{
    public const string CameraName = "PTZ Simulator";

    private readonly SimulatedClock clock;
    private readonly bool continuousMoves;
    private readonly SimulatedCameraQuirks quirks;

    public SimulatorDevices(SimulatedClock clock, bool continuousMoves = false, SimulatedCameraQuirks quirks = SimulatedCameraQuirks.None)
    {
        this.clock = clock;
        this.continuousMoves = continuousMoves;
        this.quirks = quirks;
    }

    /// <summary>The camera of the last session started.</summary>
    public SimulatedCamera? Camera { get; private set; }

    public IReadOnlyList<VideoDevice> GetDevices() => new[] { new VideoDevice(0, CameraName) };

    public IVideoSession Start(VideoDevice device, bool preview)
    {
        Camera = new SimulatedCamera(clock.Create(SimulatedCamera.TickInterval), continuousMoves, quirks);
        return new Session(device, Camera);
    }

    public IReadOnlyList<string> GetPortNames() => Array.Empty<string>();

    public IByteTransport Open(string portName, int baudRate) => throw new NotSupportedException();

    private sealed class Session : IVideoSession
    {
        private readonly SimulatedCamera camera;

        public Session(VideoDevice device, SimulatedCamera camera)
        {
            Device = device;
            this.camera = camera;
        }

        public VideoDevice Device { get; }

        public ICameraAxes? Axes => camera;

        public void Dispose() => camera.Dispose();
    }
}
