using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace TenveoPtz.App.Views.Theming;

/// <summary>Drawing helpers for rounded, anti-aliased Fluent shapes.</summary>
internal static class Painting
{
    public static void UseHighQuality(this Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    }

    public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRounded(this Graphics graphics, Color color, RectangleF bounds, float radius)
    {
        if (color.A == 0)
        {
            return;
        }

        using var path = RoundedRectangle(bounds, radius);
        using var brush = new SolidBrush(color);
        graphics.FillPath(brush, path);
    }

    /// <summary>Strokes a rounded rectangle whose outer edge lies on <paramref name="bounds"/>.</summary>
    public static void DrawRounded(this Graphics graphics, Color color, RectangleF bounds, float radius, float width = 1F)
    {
        var inset = width / 2F;
        var inner = RectangleF.Inflate(bounds, -inset, -inset);
        using var path = RoundedRectangle(inner, Math.Max(0, radius - inset));
        using var pen = new Pen(color, width);
        graphics.DrawPath(pen, path);
    }

    public static void FillCircle(this Graphics graphics, Color color, PointF center, float radius)
    {
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, center.X - radius, center.Y - radius, radius * 2, radius * 2);
    }

    public static void DrawCircle(this Graphics graphics, Color color, PointF center, float radius, float width = 1F)
    {
        using var pen = new Pen(color, width);
        var r = radius - (width / 2F);
        graphics.DrawEllipse(pen, center.X - r, center.Y - r, r * 2, r * 2);
    }

    /// <summary>Draws text with GDI (ClearType, matching native controls) inside <paramref name="bounds"/>.</summary>
    public static void DrawText(this Graphics graphics, string text, Font font, Color color, Rectangle bounds, TextFormatFlags alignment)
    {
        TextRenderer.DrawText(graphics, text, font, bounds, color, alignment | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    /// <summary>Draws a glyph centred on <paramref name="center"/>, optionally rotated (GDI+, supports transforms).</summary>
    public static void DrawGlyph(this Graphics graphics, string glyph, Font font, Color color, PointF center, float rotation = 0F)
    {
        var state = graphics.Save();
        graphics.TranslateTransform(center.X, center.Y);
        graphics.RotateTransform(rotation);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        graphics.DrawString(glyph, font, brush, PointF.Empty, format);
        graphics.Restore(state);
    }
}
