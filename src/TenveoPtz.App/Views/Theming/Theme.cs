using System;
using Microsoft.Win32;

namespace TenveoPtz.App.Views.Theming;

/// <summary>
/// Ambient theme of the application, like <see cref="System.Drawing.SystemColors"/>: controls read
/// <see cref="Current"/> when painting and repaint on <see cref="Changed"/>.
/// </summary>
internal static class Theme
{
    private static ThemeMode mode = ThemeMode.System;
    private static bool watching;

    public static event EventHandler? Changed;

    public static Palette Current { get; private set; } = Palette.Light(SystemThemeReader.Accent(dark: false));

    public static void Initialize(ThemeMode requested)
    {
        mode = requested;
        Reload();
    }

    /// <summary>
    /// Follows Windows theme and accent changes. Call on the UI thread once a window exists, so
    /// the notifications are delivered on that thread.
    /// </summary>
    public static void StartWatching()
    {
        if (!watching)
        {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            watching = true;
        }
    }

    public static void StopWatching()
    {
        if (watching)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            watching = false;
        }
    }

    private static void Reload()
    {
        var dark = mode == ThemeMode.Dark || (mode == ThemeMode.System && SystemThemeReader.AppsUseDarkTheme());
        var accent = SystemThemeReader.Accent(dark);
        Current = dark ? Palette.Dark(accent) : Palette.Light(accent);
        Changed?.Invoke(null, EventArgs.Empty);
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.VisualStyle)
        {
            Reload();
        }
    }
}
