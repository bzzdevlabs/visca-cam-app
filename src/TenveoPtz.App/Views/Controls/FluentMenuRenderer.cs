using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Renders context menus like Windows 11 flyouts: rounded hover fill and an accent pill for the current item.</summary>
internal sealed class FluentMenuRenderer : ToolStripRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.Current.Surface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Theme.Current.ControlBorder);
        e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.UseHighQuality();
        var scale = e.ToolStrip.DeviceDpi / 96F;
        var bounds = new RectangleF(4 * scale, 1, e.Item.Width - (8 * scale), e.Item.Height - 2);
        if (e.Item.Selected && e.Item.Enabled)
        {
            graphics.FillRounded(Theme.Current.SubtleHover, bounds, 4 * scale);
        }

        if (e.Item is ToolStripMenuItem { Checked: true })
        {
            var pill = new RectangleF(bounds.X, bounds.Y + (bounds.Height / 2F) - (8 * scale), 3 * scale, 16 * scale);
            graphics.FillRounded(Theme.Current.Accent, pill, 1.5F * scale);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Theme.Current.Text : Theme.Current.TextDisabled;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        // The accent pill drawn with the background replaces the classic check mark.
    }
}
