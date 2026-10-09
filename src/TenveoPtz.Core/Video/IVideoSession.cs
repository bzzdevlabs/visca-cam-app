using System;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Video;

/// <summary>A running live preview. Disposing it stops the preview and releases the camera.</summary>
public interface IVideoSession : IDisposable
{
    VideoDevice Device { get; }

    /// <summary>UVC camera controls of the device, or <c>null</c> when the driver exposes none.</summary>
    ICameraAxes? Axes { get; }
}
