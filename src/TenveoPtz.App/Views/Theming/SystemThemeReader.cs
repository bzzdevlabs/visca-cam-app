using System;
using System.Drawing;
using System.Security;
using Microsoft.Win32;

namespace TenveoPtz.App.Views.Theming;

/// <summary>Reads the user's Windows personalisation: light/dark app mode and accent colour.</summary>
internal static class SystemThemeReader
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AccentKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";

    // AccentPalette holds 8 RGBA shades from lightest to darkest. Windows 11 uses the
    // "dark 1" shade on light backgrounds and "light 2" on dark ones.
    private const int LightThemeShade = 4;
    private const int DarkThemeShade = 1;

    private static readonly Color DefaultLightAccent = Color.FromArgb(0x00, 0x5F, 0xB8);
    private static readonly Color DefaultDarkAccent = Color.FromArgb(0x60, 0xCD, 0xFF);

    public static bool AppsUseDarkTheme() => ReadValue(PersonalizeKey, "AppsUseLightTheme") is int light && light == 0;

    public static Color Accent(bool dark)
    {
        if (ReadValue(AccentKey, "AccentPalette") is byte[] shades && shades.Length >= 32)
        {
            var offset = (dark ? DarkThemeShade : LightThemeShade) * 4;
            return Color.FromArgb(shades[offset], shades[offset + 1], shades[offset + 2]);
        }

        return dark ? DefaultDarkAccent : DefaultLightAccent;
    }

    private static object? ReadValue(string keyPath, string name)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            return key?.GetValue(name);
        }
        catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
        {
            return null;
        }
    }
}
