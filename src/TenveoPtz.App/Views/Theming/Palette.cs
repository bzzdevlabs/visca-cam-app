using System.Drawing;

namespace TenveoPtz.App.Views.Theming;

/// <summary>Colour set of one theme, modelled on the Windows 11 (Fluent) design tokens.</summary>
internal sealed class Palette
{
    private Palette()
    {
    }

    public bool IsDark { get; private set; }

    /// <summary>Window background (stands in for Mica).</summary>
    public Color Background { get; private set; }

    /// <summary>Card background.</summary>
    public Color Surface { get; private set; }

    public Color SurfaceBorder { get; private set; }

    public Color ControlFill { get; private set; }

    public Color ControlFillHover { get; private set; }

    public Color ControlFillPressed { get; private set; }

    public Color ControlFillDisabled { get; private set; }

    public Color ControlBorder { get; private set; }

    /// <summary>Strong neutral used for slider rails and switch outlines.</summary>
    public Color ControlStrong { get; private set; }

    /// <summary>Translucent overlay for hovered subtle buttons and list items.</summary>
    public Color SubtleHover { get; private set; }

    public Color SubtlePressed { get; private set; }

    public Color Text { get; private set; }

    public Color TextSecondary { get; private set; }

    public Color TextDisabled { get; private set; }

    public Color Accent { get; private set; }

    public Color AccentHover { get; private set; }

    public Color AccentPressed { get; private set; }

    /// <summary>Text and glyphs drawn on an accent fill.</summary>
    public Color OnAccent { get; private set; }

    public Color Error { get; private set; }

    public Color VideoBackground { get; private set; }

    public static Palette Light(Color accent) => new()
    {
        IsDark = false,
        Background = Rgb(0xF3F3F3),
        Surface = Rgb(0xFBFBFB),
        SurfaceBorder = Rgb(0xE5E5E5),
        ControlFill = Rgb(0xFEFEFE),
        ControlFillHover = Rgb(0xF6F6F6),
        ControlFillPressed = Rgb(0xF0F0F0),
        ControlFillDisabled = Rgb(0xF5F5F5),
        ControlBorder = Rgb(0xDADADA),
        ControlStrong = Rgb(0x8A8A8A),
        SubtleHover = Color.FromArgb(0x0F, 0, 0, 0),
        SubtlePressed = Color.FromArgb(0x09, 0, 0, 0),
        Text = Rgb(0x1B1B1B),
        TextSecondary = Rgb(0x5D5D5D),
        TextDisabled = Rgb(0xA0A0A0),
        Accent = accent,
        AccentHover = Blend(accent, Color.White, 0.10),
        AccentPressed = Blend(accent, Color.White, 0.20),
        OnAccent = Color.White,
        Error = Rgb(0xC42B1C),
        VideoBackground = Rgb(0x1C1C1C),
    };

    public static Palette Dark(Color accent) => new()
    {
        IsDark = true,
        Background = Rgb(0x202020),
        Surface = Rgb(0x2B2B2B),
        SurfaceBorder = Rgb(0x1C1C1C),
        ControlFill = Rgb(0x373737),
        ControlFillHover = Rgb(0x3D3D3D),
        ControlFillPressed = Rgb(0x323232),
        ControlFillDisabled = Rgb(0x303030),
        ControlBorder = Rgb(0x454545),
        ControlStrong = Rgb(0x9E9E9E),
        SubtleHover = Color.FromArgb(0x0F, 0xFF, 0xFF, 0xFF),
        SubtlePressed = Color.FromArgb(0x0A, 0xFF, 0xFF, 0xFF),
        Text = Color.White,
        TextSecondary = Rgb(0xC8C8C8),
        TextDisabled = Rgb(0x787878),
        Accent = accent,
        AccentHover = Blend(accent, Rgb(0x202020), 0.10),
        AccentPressed = Blend(accent, Rgb(0x202020), 0.20),
        OnAccent = Color.Black,
        Error = Rgb(0xFF99A4),
        VideoBackground = Rgb(0x0F0F0F),
    };

    /// <summary>Mixes <paramref name="amount"/> (0..1) of <paramref name="other"/> into <paramref name="color"/>.</summary>
    public static Color Blend(Color color, Color other, double amount) => Color.FromArgb(
        color.A,
        (int)(color.R + ((other.R - color.R) * amount)),
        (int)(color.G + ((other.G - color.G) * amount)),
        (int)(color.B + ((other.B - color.B) * amount)));

    private static Color Rgb(int rgb) => Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
}
