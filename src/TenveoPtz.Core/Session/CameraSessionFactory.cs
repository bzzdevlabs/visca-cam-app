using System;
using System.Linq;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Execution;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Transport;
using TenveoPtz.Core.Video;
using TenveoPtz.Core.Visca;

namespace TenveoPtz.Core.Session;

/// <summary>Builds a <see cref="CameraSession"/>, choosing the PTZ strategy from the connection mode.</summary>
public sealed class CameraSessionFactory : ICameraSessionFactory
{
    /// <summary>
    /// Interval between position steps when emulating continuous UVC moves: short, so the camera
    /// gets many small steps and the motion looks continuous rather than jerky.
    /// </summary>
    public static readonly TimeSpan UvcTickInterval = TimeSpan.FromMilliseconds(40);

    private readonly IVideoService videoService;
    private readonly ISerialPortProvider serialPorts;
    private readonly ITickerFactory tickers;
    private readonly Logger log;

    public CameraSessionFactory(IVideoService videoService, ISerialPortProvider serialPorts, ITickerFactory tickers, Logger? log = null)
    {
        this.videoService = videoService ?? throw new ArgumentNullException(nameof(videoService));
        this.serialPorts = serialPorts ?? throw new ArgumentNullException(nameof(serialPorts));
        this.tickers = tickers ?? throw new ArgumentNullException(nameof(tickers));
        this.log = log ?? Logger.Null;
    }

    public CameraSession Open(ConnectionOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        IVideoSession? video = null;
        try
        {
            // VISCA needs nothing from the video device, so without a preview it is left alone entirely.
            var needsVideo = options.ShowPreview || options.Mode == ControlMode.Uvc;
            video = needsVideo ? StartVideo(options.VideoDeviceName, options.ShowPreview) : null;
            var ptz = options.Mode == ControlMode.Visca ? CreateVisca(options) : CreateUvc(video, options.UvcContinuousMoves);
            return new CameraSession(video, ptz);
        }
        catch
        {
            video?.Dispose();
            throw;
        }
    }

    private IVideoSession? StartVideo(string deviceName, bool preview)
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            return null;
        }

        var device = videoService.GetDevices().FirstOrDefault(d => d.Name == deviceName)
            ?? throw new InvalidOperationException($"Video device \"{deviceName}\" is not connected.");
        var video = videoService.Start(device, preview);
        log.Info($"{(preview ? "Video preview started on" : "Opened without preview")} \"{deviceName}\" ({(video.Axes is null ? "no" : "with")} UVC camera controls).");
        return video;
    }

    private IPtzController CreateVisca(ConnectionOptions options)
    {
        if (string.IsNullOrEmpty(options.PortName))
        {
            throw new InvalidOperationException("Select the COM port the camera's VISCA cable is connected to.");
        }

        var transport = serialPorts.Open(options.PortName, options.BaudRate);
        try
        {
            var viscaLog = log.For("VISCA");
            var client = new ViscaClient(transport, options.Address, viscaLog);
            var executor = new BackgroundCommandExecutor("VISCA " + options.PortName);
            log.Info($"Opened {options.PortName} at {options.BaudRate} baud, VISCA address {options.Address}.");
            return new ViscaPtzController(client, executor, $"VISCA on {options.PortName}", viscaLog);
        }
        catch
        {
            transport.Dispose();
            throw;
        }
    }

    private IPtzController CreateUvc(IVideoSession? video, bool continuousMoves)
    {
        var axes = video?.Axes
            ?? throw new InvalidOperationException(
                video is null
                    ? "UVC control needs the camera's video device: select it in the Camera list."
                    : "This video device does not expose UVC pan/tilt/zoom controls. Use VISCA instead.");
        foreach (CameraAxis axis in Enum.GetValues(typeof(CameraAxis)))
        {
            var range = axes.GetRange(axis);
            var speeds = axis == CameraAxis.Focus ? null : axes.GetSpeedRange(axis);
            log.Info(range is null
                ? $"UVC {axis}: not supported."
                : $"UVC {axis}: {range.Min}..{range.Max}, step {range.Step}, default {range.Default}, current {axes.GetValue(axis)?.ToString() ?? "unknown"}, "
                    + (speeds is null ? "absolute moves only." : $"continuous speeds {speeds.Min}..{speeds.Max}."));
        }

        var controller = new UvcPtzController(axes, tickers.Create(UvcTickInterval), continuousMoves);
        log.Info(continuousMoves
            ? $"UVC moves: continuous where available (pan {Mode(CameraAxis.Pan)}, tilt {Mode(CameraAxis.Tilt)}, zoom {Mode(CameraAxis.Zoom)})."
            : "UVC moves: absolute steps (continuous moves are off; UvcContinuousMoves in settings.xml turns them on).");
        return controller;

        string Mode(CameraAxis axis) => controller.MovesContinuously(axis) ? "continuous" : "steps";
    }
}
