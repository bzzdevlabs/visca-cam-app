using System;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Presentation;

public sealed class MotionEventArgs : EventArgs
{
    public MotionEventArgs(Motion direction)
    {
        Direction = direction;
    }

    public Motion Direction { get; }
}
