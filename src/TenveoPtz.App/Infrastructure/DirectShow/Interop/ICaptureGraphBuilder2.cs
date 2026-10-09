using System;
using System.Runtime.InteropServices;

namespace TenveoPtz.App.Infrastructure.DirectShow.Interop;

/// <summary>Capture graph helper. Only the leading methods are declared (vtable order preserved).</summary>
[ComImport]
[Guid("93E5A4E0-2D50-11d2-ABFA-00A0C9C6E38D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ICaptureGraphBuilder2
{
    [PreserveSig]
    int SetFiltergraph([In] IGraphBuilder graph);

    [PreserveSig]
    int GetFiltergraph(out IGraphBuilder graph);

    [PreserveSig]
    int SetOutputFileName(IntPtr type, IntPtr file, IntPtr multiplexer, IntPtr sink);

    [PreserveSig]
    int FindInterface(IntPtr category, IntPtr type, [In] IBaseFilter filter, [In] ref Guid interfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object found);

    [PreserveSig]
    int RenderStream([In] ref Guid category, [In] ref Guid mediaType, [In, MarshalAs(UnmanagedType.IUnknown)] object source, [In] IBaseFilter? compressor, [In] IBaseFilter? renderer);
}
