using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>
/// Rounded host for the live video. The window region clips the DirectShow renderer to the
/// rounded shape; a placeholder is drawn while no video is running.
/// </summary>
internal sealed class VideoSurface : Panel
{
    private const float CornerRadius = 8F;

    private bool showPlaceholder = true;

    public VideoSurface()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Current.VideoBackground;
        Theme.Changed += OnThemeChanged;
    }

    public string PlaceholderTitle { get; set; } = string.Empty;

    public string PlaceholderText { get; set; } = string.Empty;

    public bool ShowPlaceholder
    {
        get => showPlaceholder;
        set
        {
            showPlaceholder = value;
            Invalidate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
            Region?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        var previous = Region;
        using (var path = Painting.RoundedRectangle(new RectangleF(0, 0, Width, Height), CornerRadius * DeviceDpi / 96F))
        {
            Region = new Region(path);
        }

        previous?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!showPlaceholder)
        {
            return;
        }

        var graphics = e.Graphics;
        graphics.UseHighQuality();
        var scale = DeviceDpi / 96F;
        var muted = Color.FromArgb(0x9A, 0x9A, 0x9A);
        var middle = new PointF(Width / 2F, (Height / 2F) - (28 * scale));
        graphics.DrawGlyph(Glyphs.Video, FluentFonts.Icon(40), muted, middle);

        var titleTop = (int)(middle.Y + (36 * scale));
        graphics.DrawText(PlaceholderTitle, FluentFonts.BodyStrong, Color.FromArgb(0xE0, 0xE0, 0xE0), new Rectangle(0, titleTop, Width, (int)(24 * scale)), TextFormatFlags.HorizontalCenter);
        graphics.DrawText(PlaceholderText, FluentFonts.Caption, muted, new Rectangle(0, titleTop + (int)(26 * scale), Width, (int)(20 * scale)), TextFormatFlags.HorizontalCenter);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        BackColor = Theme.Current.VideoBackground;
        Invalidate();
    }
}
