using System;
using System.Windows.Forms;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Execution;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Simulation;
using TenveoPtz.Core.Video;

namespace TenveoPtz.App.Infrastructure.Simulation;

/// <summary>Simulated camera and, with a preview, its live picture in the video area.</summary>
internal sealed class SimulatedVideoSession : IVideoSession
{
    private readonly SimulatedCamera camera;
    private readonly SimulatorView? view;
    private readonly Logger log;

    public SimulatedVideoSession(VideoDevice device, Control host, ITicker ticker, SimulatedCameraQuirks quirks, bool preview, Logger log)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        this.log = log;
        camera = new SimulatedCamera(ticker, continuousMoves: true, quirks);
        camera.Settled += OnSettled;
        log.Info($"Simulated camera started (quirks: {quirks}). {Describe()}");
        if (preview)
        {
            view = new SimulatorView(camera) { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            view.BringToFront();
        }
    }

    public VideoDevice Device { get; }

    public ICameraAxes? Axes => camera;

    public void Dispose()
    {
        camera.Settled -= OnSettled;
        if (view is not null)
        {
            view.Parent?.Controls.Remove(view);
            view.Dispose();
        }

        camera.Dispose();
    }

    /// <summary>Logged on every stop, so tests (and people) can tell where the camera ended up.</summary>
    private void OnSettled(object? sender, EventArgs e) => log.Info(Describe());

    private string Describe() =>
        $"Simulator at pan {camera.GetValue(CameraAxis.Pan)}, tilt {camera.GetValue(CameraAxis.Tilt)}, zoom {camera.GetValue(CameraAxis.Zoom)}.";
}
