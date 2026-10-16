using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Execution;
using TenveoPtz.Core.Simulation;
using TenveoPtz.Core.Video;

namespace TenveoPtz.App.Infrastructure.Simulation;

/// <summary>
/// Adds the "PTZ Simulator" camera (app started with <c>--simulator</c>) to the real video
/// devices: a <see cref="SimulatedCamera"/> with a live picture, to try and test the app without
/// hardware. Other devices are handled by the wrapped service.
/// </summary>
internal sealed class SimulatorVideoService : IVideoService
{
    public const string DeviceName = "PTZ Simulator";

    private readonly IVideoService inner;
    private readonly Control host;
    private readonly ITickerFactory tickers;
    private readonly SimulatedCameraQuirks quirks;
    private readonly Logger log;

    public SimulatorVideoService(IVideoService inner, Control host, ITickerFactory tickers, SimulatedCameraQuirks quirks, Logger log)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.tickers = tickers ?? throw new ArgumentNullException(nameof(tickers));
        this.quirks = quirks;
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public IReadOnlyList<VideoDevice> GetDevices()
    {
        IEnumerable<VideoDevice> real;
        try
        {
            real = inner.GetDevices();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A machine without DirectShow devices (CI) still gets the simulator.
            log.Warning("Real video devices unavailable: " + ex.Message);
            real = Array.Empty<VideoDevice>();
        }

        return new[] { new VideoDevice(-1, DeviceName) }.Concat(real).ToList();
    }

    public IVideoSession Start(VideoDevice device, bool preview) =>
        device.Name == DeviceName
            ? new SimulatedVideoSession(device, host, tickers.Create(SimulatedCamera.TickInterval), quirks, preview, log)
            : inner.Start(device, preview);
}
