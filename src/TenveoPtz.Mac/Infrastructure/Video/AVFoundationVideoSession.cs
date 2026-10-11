using System;
using AVFoundation;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Video;
using TenveoPtz.Mac.Infrastructure.Uvc;
using TenveoPtz.Mac.Views.Controls;

namespace TenveoPtz.Mac.Infrastructure.Video;

/// <summary>
/// Live preview through an AVFoundation capture session and its preview layer: frames go
/// straight from the camera to the screen compositor, so the preview costs almost no CPU.
/// </summary>
internal sealed class AVFoundationVideoSession : IVideoSession
{
    private readonly VideoView host;
    private readonly AVCaptureSession session = new();
    private readonly AVCaptureVideoPreviewLayer preview;
    private readonly UvcCameraAxes? axes;

    public AVFoundationVideoSession(VideoDevice device, AVCaptureDevice capture, VideoView host)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        EnsureCameraAccess();

        var input = AVCaptureDeviceInput.FromDevice(capture, out var error)
            ?? throw new InvalidOperationException($"Cannot open \"{device.Name}\": {error?.LocalizedDescription ?? "unknown error"}.");
        if (!session.CanAddInput(input))
        {
            input.Dispose();
            session.Dispose();
            throw new InvalidOperationException($"\"{device.Name}\" is in use by another application.");
        }

        session.AddInput(input);
        preview = new AVCaptureVideoPreviewLayer(session) { VideoGravity = AVLayerVideoGravity.ResizeAspect };
        host.ShowPreview(preview);
        session.StartRunning();

        if (UsbLocation.TryParse(capture.UniqueID, out var location) && UvcDevice.Open(location) is { } uvc)
        {
            axes = new UvcCameraAxes(uvc);
        }
    }

    public VideoDevice Device { get; }

    public ICameraAxes? Axes => axes;

    public void Dispose()
    {
        session.StopRunning();
        host.ShowPreview(null);
        preview.Dispose();
        foreach (var input in session.Inputs)
        {
            session.RemoveInput(input);
        }

        session.Dispose();
        axes?.Dispose();
    }

    private static void EnsureCameraAccess()
    {
        switch (AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video))
        {
            case AVAuthorizationStatus.Denied:
            case AVAuthorizationStatus.Restricted:
                throw new UnauthorizedAccessException(
                    "Tenveo PTZ may not use the camera. Allow it in System Settings > Privacy & Security > Camera, then restart the app.");
            case AVAuthorizationStatus.NotDetermined:
                AVCaptureDevice.RequestAccessForMediaType(AVAuthorizationMediaType.Video, _ => { });
                throw new UnauthorizedAccessException("Allow Tenveo PTZ to use the camera, then press Connect again.");
        }
    }
}
