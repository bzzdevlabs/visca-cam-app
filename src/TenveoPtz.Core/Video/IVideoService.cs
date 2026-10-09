using System.Collections.Generic;

namespace TenveoPtz.Core.Video;

/// <summary>Lists capture devices and starts live previews in the application's video area.</summary>
public interface IVideoService
{
    IReadOnlyList<VideoDevice> GetDevices();

    IVideoSession Start(VideoDevice device);
}
