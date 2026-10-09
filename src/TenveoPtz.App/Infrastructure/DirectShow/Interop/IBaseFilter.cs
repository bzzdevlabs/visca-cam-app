using System.Runtime.InteropServices;

namespace TenveoPtz.App.Infrastructure.DirectShow.Interop;

/// <summary>
/// DirectShow filter. Declared without members on purpose: the filter is only passed to the
/// graph builders and queried for other interfaces, never called directly.
/// </summary>
[ComImport]
[Guid("56a86895-0ad4-11ce-b03a-0020af0ba770")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IBaseFilter
{
}
