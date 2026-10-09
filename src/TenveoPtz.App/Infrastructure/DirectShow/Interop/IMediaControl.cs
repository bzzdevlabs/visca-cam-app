using System.Runtime.InteropServices;

namespace TenveoPtz.App.Infrastructure.DirectShow.Interop;

/// <summary>Graph run/stop control. Only the leading methods are declared (vtable order preserved).</summary>
[ComImport]
[Guid("56a868b1-0ad4-11ce-b03a-0020af0ba770")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
internal interface IMediaControl
{
    [PreserveSig]
    int Run();

    [PreserveSig]
    int Pause();

    [PreserveSig]
    int Stop();
}
