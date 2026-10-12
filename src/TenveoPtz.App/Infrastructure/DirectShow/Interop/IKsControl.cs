using System;
using System.Runtime.InteropServices;

namespace TenveoPtz.App.Infrastructure.DirectShow.Interop;

/// <summary>Kernel streaming property access of a capture filter (ks.h), for the camera controls IAMCameraControl does not cover.</summary>
[ComImport]
[Guid("28F54685-06FD-11D2-B27A-00A0C9223196")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IKsControl
{
    [PreserveSig]
    int KsProperty(IntPtr property, int propertyLength, IntPtr propertyData, int dataLength, out int bytesReturned);

    [PreserveSig]
    int KsMethod(IntPtr method, int methodLength, IntPtr methodData, int dataLength, out int bytesReturned);

    [PreserveSig]
    int KsEvent(IntPtr ksEvent, int eventLength, IntPtr eventData, int dataLength, out int bytesReturned);
}
