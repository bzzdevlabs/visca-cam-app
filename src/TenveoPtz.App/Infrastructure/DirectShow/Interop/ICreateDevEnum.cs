using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace TenveoPtz.App.Infrastructure.DirectShow.Interop;

[ComImport]
[Guid("29840822-5B84-11D0-BD3B-00A0C911CE86")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ICreateDevEnum
{
    /// <returns>S_OK, or S_FALSE (1) with a null enumerator when the category is empty.</returns>
    [PreserveSig]
    int CreateClassEnumerator([In] ref Guid deviceClass, out IEnumMoniker? enumMoniker, [In] int flags);
}
