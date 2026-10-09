using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace TenveoPtz.App.Infrastructure.DirectShow.Interop;

/// <summary>
/// Video renderer window. Every method up to <see cref="SetWindowPosition"/> is declared so
/// the vtable lines up; the COM property accessor names are kept as in control.h.
/// </summary>
[ComImport]
[Guid("56a868b4-0ad4-11ce-b03a-0020af0ba770")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Mirrors the COM interface definition.")]
internal interface IVideoWindow
{
    [PreserveSig] int put_Caption([In, MarshalAs(UnmanagedType.BStr)] string caption);

    [PreserveSig] int get_Caption([MarshalAs(UnmanagedType.BStr)] out string caption);

    [PreserveSig] int put_WindowStyle(int style);

    [PreserveSig] int get_WindowStyle(out int style);

    [PreserveSig] int put_WindowStyleEx(int style);

    [PreserveSig] int get_WindowStyleEx(out int style);

    [PreserveSig] int put_AutoShow(int autoShow);

    [PreserveSig] int get_AutoShow(out int autoShow);

    [PreserveSig] int put_WindowState(int state);

    [PreserveSig] int get_WindowState(out int state);

    [PreserveSig] int put_BackgroundPalette(int palette);

    [PreserveSig] int get_BackgroundPalette(out int palette);

    [PreserveSig] int put_Visible(int visible);

    [PreserveSig] int get_Visible(out int visible);

    [PreserveSig] int put_Left(int left);

    [PreserveSig] int get_Left(out int left);

    [PreserveSig] int put_Width(int width);

    [PreserveSig] int get_Width(out int width);

    [PreserveSig] int put_Top(int top);

    [PreserveSig] int get_Top(out int top);

    [PreserveSig] int put_Height(int height);

    [PreserveSig] int get_Height(out int height);

    [PreserveSig] int put_Owner(IntPtr owner);

    [PreserveSig] int get_Owner(out IntPtr owner);

    [PreserveSig] int put_MessageDrain(IntPtr drain);

    [PreserveSig] int get_MessageDrain(out IntPtr drain);

    [PreserveSig] int get_BorderColor(out int color);

    [PreserveSig] int put_BorderColor(int color);

    [PreserveSig] int get_FullScreenMode(out int fullScreen);

    [PreserveSig] int put_FullScreenMode(int fullScreen);

    [PreserveSig] int SetWindowForeground(int focus);

    [PreserveSig] int NotifyOwnerMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [PreserveSig] int SetWindowPosition(int left, int top, int width, int height);
}
