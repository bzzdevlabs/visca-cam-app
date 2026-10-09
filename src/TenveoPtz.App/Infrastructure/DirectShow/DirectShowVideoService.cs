using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TenveoPtz.Core.Video;

namespace TenveoPtz.App.Infrastructure.DirectShow;

/// <summary>Video service backed by DirectShow, rendering previews into a host control.</summary>
internal sealed class DirectShowVideoService : IVideoService
{
    private readonly Control host;

    public DirectShowVideoService(Control host)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public IReadOnlyList<VideoDevice> GetDevices() =>
        DeviceEnumerator.GetDeviceNames().Select((name, index) => new VideoDevice(index, name)).ToList();

    public IVideoSession Start(VideoDevice device) => new DirectShowVideoSession(device, host);
}
