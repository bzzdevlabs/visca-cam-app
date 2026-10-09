using System;
using System.Runtime.InteropServices;

namespace TenveoPtz.App.Infrastructure.DirectShow.Interop;

/// <summary>Filter graph manager. Methods must stay in vtable order (IFilterGraph then IGraphBuilder).</summary>
[ComImport]
[Guid("56a868a9-0ad4-11ce-b03a-0020af0ba770")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGraphBuilder
{
    [PreserveSig]
    int AddFilter([In] IBaseFilter filter, [In, MarshalAs(UnmanagedType.LPWStr)] string name);

    [PreserveSig]
    int RemoveFilter([In] IBaseFilter filter);

    [PreserveSig]
    int EnumFilters(out IntPtr enumFilters);

    [PreserveSig]
    int FindFilterByName([In, MarshalAs(UnmanagedType.LPWStr)] string name, out IBaseFilter filter);

    [PreserveSig]
    int ConnectDirect(IntPtr pinOut, IntPtr pinIn, IntPtr mediaType);

    [PreserveSig]
    int Reconnect(IntPtr pin);

    [PreserveSig]
    int Disconnect(IntPtr pin);

    [PreserveSig]
    int SetDefaultSyncSource();

    [PreserveSig]
    int Connect(IntPtr pinOut, IntPtr pinIn);

    [PreserveSig]
    int Render(IntPtr pinOut);

    [PreserveSig]
    int RenderFile([In, MarshalAs(UnmanagedType.LPWStr)] string file, [In, MarshalAs(UnmanagedType.LPWStr)] string playList);

    [PreserveSig]
    int AddSourceFilter([In, MarshalAs(UnmanagedType.LPWStr)] string fileName, [In, MarshalAs(UnmanagedType.LPWStr)] string filterName, out IBaseFilter filter);

    [PreserveSig]
    int SetLogFile(IntPtr file);

    [PreserveSig]
    int Abort();

    [PreserveSig]
    int ShouldOperationContinue();
}
