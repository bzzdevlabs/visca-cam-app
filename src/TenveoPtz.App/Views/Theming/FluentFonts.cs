using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;

namespace TenveoPtz.App.Views.Theming;

/// <summary>Windows 11 type ramp, falling back to the Windows 10 fonts when needed.</summary>
internal static class FluentFonts
{
    private static readonly HashSet<string> Installed = InstalledFamilies();
    private static readonly string TextFamily = FirstInstalled("Segoe UI Variable Text", "Segoe UI");
    private static readonly string DisplayFamily = FirstInstalled("Segoe UI Variable Display", "Segoe UI");
    private static readonly string IconFamily = FirstInstalled("Segoe Fluent Icons", "Segoe MDL2 Assets");
    private static readonly Dictionary<float, Font> IconCache = new();

    /// <summary>Body text (14 px).</summary>
    public static Font Body { get; } = new(TextFamily, 10.5F, GraphicsUnit.Point);

    /// <summary>Emphasised body text (14 px semibold).</summary>
    public static Font BodyStrong { get; } = new(TextFamily, 10.5F, FontStyle.Bold, GraphicsUnit.Point);

    /// <summary>Caption text (12 px).</summary>
    public static Font Caption { get; } = new(TextFamily, 9F, GraphicsUnit.Point);

    /// <summary>Section titles (20 px semibold).</summary>
    public static Font Subtitle { get; } = new(DisplayFamily, 15F, FontStyle.Bold, GraphicsUnit.Point);

    /// <summary>Icon font at <paramref name="pixelSize"/> logical pixels.</summary>
    public static Font Icon(float pixelSize)
    {
        if (!IconCache.TryGetValue(pixelSize, out var font))
        {
            font = new Font(IconFamily, pixelSize * 0.75F, GraphicsUnit.Point);
            IconCache[pixelSize] = font;
        }

        return font;
    }

    private static HashSet<string> InstalledFamilies()
    {
        using var fonts = new InstalledFontCollection();
        return new HashSet<string>(fonts.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
    }

    private static string FirstInstalled(params string[] families) =>
        families.FirstOrDefault(Installed.Contains) ?? families[families.Length - 1];
}
