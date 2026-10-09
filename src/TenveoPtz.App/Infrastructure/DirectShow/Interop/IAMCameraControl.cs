using System.Runtime.InteropServices;

namespace TenveoPtz.App.Infrastructure.DirectShow.Interop;

/// <summary>UVC camera terminal controls (pan, tilt, zoom, focus...) exposed by the capture filter.</summary>
[ComImport]
[Guid("C6E13370-30AC-11d0-A18C-00A0C9118956")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAMCameraControl
{
    [PreserveSig]
    int GetRange(CameraControlProperty property, out int min, out int max, out int steppingDelta, out int defaultValue, out CameraControlFlags capabilities);

    [PreserveSig]
    int Set(CameraControlProperty property, int value, CameraControlFlags flags);

    [PreserveSig]
    int Get(CameraControlProperty property, out int value, out CameraControlFlags flags);
}
