using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TenveoPtz.App.Views.Theming;

/// <summary>
/// Windows 11 window chrome through the Desktop Window Manager: dark title bar, caption colour
/// and rounded corners. Calls are ignored on systems that do not support an attribute.
/// </summary>
internal static class WindowChrome
{
    private const int UseImmersiveDarkMode = 20;
    private const int WindowCornerPreference = 33;
    private const int CaptionColor = 35;
    private const int CornerRound = 2;

    public static void Apply(Form form, Palette palette)
    {
        if (!form.IsHandleCreated)
        {
            return;
        }

        SetAttribute(form.Handle, UseImmersiveDarkMode, palette.IsDark ? 1 : 0);
        SetAttribute(form.Handle, CaptionColor, ColorTranslator.ToWin32(palette.Background));
    }

    /// <summary>Gives a popup (such as a dropdown menu) the rounded corners of Windows 11.</summary>
    public static void RoundCorners(IntPtr window) => SetAttribute(window, WindowCornerPreference, CornerRound);

    /// <summary>Uses dark or light scroll bars for a native control.</summary>
    public static void ApplyScrollBars(Control control, Palette palette)
    {
        if (control.IsHandleCreated)
        {
            _ = SetWindowTheme(control.Handle, palette.IsDark ? "DarkMode_Explorer" : "Explorer", null);
        }
    }

    private static void SetAttribute(IntPtr window, int attribute, int value)
    {
        try
        {
            _ = DwmSetWindowAttribute(window, attribute, ref value, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // No desktop composition (e.g. some server configurations): keep the default chrome.
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr window, string? subAppName, string? subIdList);
}
