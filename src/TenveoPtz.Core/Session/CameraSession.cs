using System;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Video;

namespace TenveoPtz.Core.Session;

/// <summary>An open connection to the camera: live video plus a PTZ control strategy.</summary>
public sealed class CameraSession : IDisposable
{
    /// <param name="video">Video device, or <c>null</c> when none is open. Ownership is transferred.</param>
    /// <param name="ptz">PTZ strategy. Ownership is transferred.</param>
    public CameraSession(IVideoSession? video, IPtzController ptz)
    {
        Video = video;
        Ptz = ptz ?? throw new ArgumentNullException(nameof(ptz));
    }

    public IVideoSession? Video { get; }

    public IPtzController Ptz { get; }

    public string Description =>
        Video is null ? Strings.NoVideo(Ptz.Description) : $"{Video.Device.Name}, {Ptz.Description}";

    public void Dispose()
    {
        // Stop motion before releasing the control channel; a camera left moving keeps moving.
        try
        {
            Ptz.Move(Motion.None, Motion.None, 0);
            Ptz.Zoom(Motion.None, 0);
        }
        catch (Exception)
        {
            // Best effort only: the connection may already be gone.
        }

        Ptz.Dispose();
        Video?.Dispose();
    }
}
