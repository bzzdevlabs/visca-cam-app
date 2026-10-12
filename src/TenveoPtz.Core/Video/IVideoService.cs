using System.Collections.Generic;

namespace TenveoPtz.Core.Video;

/// <summary>Lists capture devices and starts live previews in the application's video area.</summary>
public interface IVideoService
{
    IReadOnlyList<VideoDevice> GetDevices();

    /// <param name="preview">
    /// <c>false</c> opens the device for its camera controls only, without receiving its video,
    /// so other applications can use the camera at the same time.
    /// </param>
    IVideoSession Start(VideoDevice device, bool preview);
}
