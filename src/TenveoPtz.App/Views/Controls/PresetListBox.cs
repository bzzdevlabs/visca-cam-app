using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>
/// Owner-drawn list of presets: Windows 11 list items with hover fill, an accent pill on the
/// selection and a number badge for the 1..9 hotkeys. Shows a hint when empty.
/// </summary>
internal sealed class PresetListBox : ListBox
{
    private const int WmPaint = 0x000F;
    private const int HotkeyCount = 9;
    private const float CornerRadius = 4F;

    private int hoveredIndex = -1;

    public PresetListBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        BorderStyle = BorderStyle.None;
        IntegralHeight = false;
        Font = FluentFonts.Body;
        ItemHeight = (int)(40 * DeviceDpi / 96F);
        ApplyTheme();
        Theme.Changed += OnThemeChanged;
    }

    /// <summary>Text shown when the list is empty.</summary>
    public string EmptyText { get; set; } = string.Empty;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowChrome.ApplyScrollBars(this, Theme.Current);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count)
        {
            return;
        }

        var colors = Theme.Current;
        var graphics = e.Graphics;
        using (var background = new SolidBrush(colors.Surface))
        {
            graphics.FillRectangle(background, e.Bounds);
        }

        graphics.UseHighQuality();
        var scale = DeviceDpi / 96F;
        var item = new RectangleF(e.Bounds.X + (2 * scale), e.Bounds.Y + (2 * scale), e.Bounds.Width - (4 * scale), e.Bounds.Height - (4 * scale));
        var selected = (e.State & DrawItemState.Selected) != 0;
        if (selected || e.Index == hoveredIndex)
        {
            graphics.FillRounded(selected ? colors.SubtleHover : colors.SubtlePressed, item, CornerRadius * scale);
        }

        if (selected)
        {
            var pill = new RectangleF(item.X, item.Y + (item.Height / 2F) - (8 * scale), 3 * scale, 16 * scale);
            graphics.FillRounded(colors.Accent, pill, 1.5F * scale);
        }

        var badge = new RectangleF(item.X + (12 * scale), item.Y + ((item.Height - (22 * scale)) / 2F), 22 * scale, 22 * scale);
        if (e.Index < HotkeyCount)
        {
            graphics.FillRounded(colors.ControlFill, badge, CornerRadius * scale);
            graphics.DrawRounded(colors.ControlBorder, badge, CornerRadius * scale);
            graphics.DrawText((e.Index + 1).ToString(), FluentFonts.Caption, colors.TextSecondary, Rectangle.Round(badge), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        var textLeft = (int)(badge.Right + (12 * scale));
        var textBounds = new Rectangle(textLeft, e.Bounds.Y, e.Bounds.Right - textLeft - (int)(8 * scale), e.Bounds.Height);
        graphics.DrawText(GetItemText(Items[e.Index]), Font, colors.Text, textBounds, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        SetHovered(IndexFromPoint(e.Location));
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        SetHovered(-1);
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WmPaint && Items.Count == 0 && EmptyText.Length > 0)
        {
            using var graphics = CreateGraphics();
            var bounds = ClientRectangle;
            bounds.Inflate(-(int)(16 * DeviceDpi / 96F), 0);
            TextRenderer.DrawText(graphics, EmptyText, FluentFonts.Caption, bounds, Theme.Current.TextSecondary, Theme.Current.Surface, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }
    }

    private void SetHovered(int index)
    {
        if (index != hoveredIndex)
        {
            hoveredIndex = index;
            Invalidate();
        }
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Current.Surface;
        ForeColor = Theme.Current.Text;
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ApplyTheme();
        WindowChrome.ApplyScrollBars(this, Theme.Current);
        Invalidate();
    }
}
