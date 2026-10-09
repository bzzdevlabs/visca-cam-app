using System;
using System.Linq;
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
    /// <summary>Interval between position steps when emulating continuous UVC moves.</summary>
    public static readonly TimeSpan UvcTickInterval = TimeSpan.FromMilliseconds(80);

    private readonly IVideoService videoService;
    private readonly ISerialPortProvider serialPorts;
    private readonly ITickerFactory tickers;

    public CameraSessionFactory(IVideoService videoService, ISerialPortProvider serialPorts, ITickerFactory tickers)
    {
        this.videoService = videoService ?? throw new ArgumentNullException(nameof(videoService));
        this.serialPorts = serialPorts ?? throw new ArgumentNullException(nameof(serialPorts));
        this.tickers = tickers ?? throw new ArgumentNullException(nameof(tickers));
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
            video = StartVideo(options.VideoDeviceName);
            var ptz = options.Mode == ControlMode.Visca ? CreateVisca(options) : CreateUvc(video);
            return new CameraSession(video, ptz);
        }
        catch
        {
            video?.Dispose();
            throw;
        }
    }

    private IVideoSession? StartVideo(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            return null;
        }

        var device = videoService.GetDevices().FirstOrDefault(d => d.Name == deviceName)
            ?? throw new InvalidOperationException($"Video device \"{deviceName}\" is not connected.");
        return videoService.Start(device);
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
            var client = new ViscaClient(transport, options.Address);
            var executor = new BackgroundCommandExecutor("VISCA " + options.PortName);
            return new ViscaPtzController(client, executor, $"VISCA on {options.PortName}");
        }
        catch
        {
            transport.Dispose();
            throw;
        }
    }

    private IPtzController CreateUvc(IVideoSession? video)
    {
        var axes = video?.Axes
            ?? throw new InvalidOperationException(
                video is null
                    ? "UVC control needs the camera's video device: select it in the Camera list."
                    : "This video device does not expose UVC pan/tilt/zoom controls. Use VISCA instead.");
        return new UvcPtzController(axes, tickers.Create(UvcTickInterval));
    }
}
