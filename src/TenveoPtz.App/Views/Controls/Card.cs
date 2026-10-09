using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Rounded surface grouping related controls, with an optional title.</summary>
internal sealed class Card : Panel
{
    private const float CornerRadius = 8F;
    private const int Inset = 14;
    private const int TitleHeight = 30;

    private readonly string? title;

    public Card(string? title = null)
    {
        this.title = title;
        DoubleBuffered = true;
        ResizeRedraw = true;
        Padding = new Padding(Inset, title is null ? 12 : Inset + TitleHeight - 4, Inset, title is null ? 12 : Inset);
        Margin = new Padding(0);
        BackColor = Theme.Current.Surface;
        Theme.Changed += OnThemeChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        using (var outside = new SolidBrush(Parent?.BackColor ?? Theme.Current.Background))
        {
            graphics.FillRectangle(outside, ClientRectangle);
        }

        graphics.UseHighQuality();
        var scale = DeviceDpi / 96F;
        var bounds = new RectangleF(0, 0, Width, Height);
        graphics.FillRounded(Theme.Current.Surface, bounds, CornerRadius * scale);
        graphics.DrawRounded(Theme.Current.SurfaceBorder, bounds, CornerRadius * scale);

        if (title is not null)
        {
            var titleBounds = new Rectangle((int)(Inset * scale), (int)(Inset * scale), Width - (int)(2 * Inset * scale), (int)(TitleHeight * scale) - (int)(Inset * scale));
            graphics.DrawText(title, FluentFonts.BodyStrong, Theme.Current.Text, titleBounds, TextFormatFlags.Left | TextFormatFlags.Top);
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        BackColor = Theme.Current.Surface;
        Invalidate();
    }
}
