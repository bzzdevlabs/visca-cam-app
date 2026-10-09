using System;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Presentation;

public sealed class PanTiltEventArgs : EventArgs
{
    public PanTiltEventArgs(Motion pan, Motion tilt)
    {
        Pan = pan;
        Tilt = tilt;
    }

    public Motion Pan { get; }

    public Motion Tilt { get; }
}
