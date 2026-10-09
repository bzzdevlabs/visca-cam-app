using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>One-line status message with an info or error icon.</summary>
internal sealed class StatusLine : FluentControl
{
    private bool isError;

    public StatusLine()
    {
        Height = 28;
        TabStop = false;
        Font = FluentFonts.Caption;
    }

    public void Show(string message, bool error)
    {
        Text = message;
        isError = error;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.UseHighQuality();
        var color = isError ? Colors.Error : Colors.TextSecondary;
        var iconCenter = new PointF(Px(12), Height / 2F);
        graphics.DrawGlyph(isError ? Glyphs.Error : Glyphs.Info, FluentFonts.Icon(14), isError ? Colors.Error : Colors.Accent, iconCenter);
        var left = (int)Px(28);
        graphics.DrawText(Text, Font, color, new Rectangle(left, 0, Width - left, Height), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }
}
