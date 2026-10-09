using System;

namespace TenveoPtz.App.Infrastructure.DirectShow.Interop;

/// <summary>Values of the CameraControlFlags enumeration (strmif.h).</summary>
[Flags]
internal enum CameraControlFlags
{
    None = 0,
    Auto = 1,
    Manual = 2,
}
