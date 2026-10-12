using System;
using System.Collections.Generic;
using System.Linq;
using AVFoundation;
using TenveoPtz.Core.Video;
using TenveoPtz.Mac.Views.Controls;

namespace TenveoPtz.Mac.Infrastructure.Video;

/// <summary>Video service backed by AVFoundation, previewing into a <see cref="VideoView"/>.</summary>
internal sealed class AVFoundationVideoService : IVideoService
{
    private readonly VideoView host;

    public AVFoundationVideoService(VideoView host)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public IReadOnlyList<VideoDevice> GetDevices() =>
        FindCaptureDevices().Select((device, index) => new VideoDevice(index, device.LocalizedName)).ToList();

    public IVideoSession Start(VideoDevice device, bool preview)
    {
        var capture = FindCaptureDevices().FirstOrDefault(d => d.LocalizedName == device.Name)
            ?? throw new InvalidOperationException($"Video device \"{device.Name}\" is not connected.");
        return new AVFoundationVideoSession(device, capture, host, preview);
    }

    /// <summary>Built-in and USB cameras (and iPhones used as Continuity Cameras).</summary>
    public static AVCaptureDevice[] FindCaptureDevices()
    {
        AVCaptureDeviceType[] types;
        if (OperatingSystem.IsMacOSVersionAtLeast(14))
        {
            types = new[] { AVCaptureDeviceType.BuiltInWideAngleCamera, AVCaptureDeviceType.External, AVCaptureDeviceType.ContinuityCamera };
        }
        else
        {
#pragma warning disable CA1422 // Replaced by External on macOS 14, which does not exist before.
            types = new[] { AVCaptureDeviceType.BuiltInWideAngleCamera, AVCaptureDeviceType.ExternalUnknown };
#pragma warning restore CA1422
        }

        using var discovery = AVCaptureDeviceDiscoverySession.Create(types, AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
        return discovery.Devices;
    }
}
