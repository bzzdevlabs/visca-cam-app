using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Windows 11 toggle switch with a label on its right.</summary>
internal sealed class ToggleSwitch : FluentControl
{
    private const float TrackWidth = 40F;
    private const float TrackHeight = 20F;
    private const float LabelGap = 12F;

    private bool isChecked;

    public ToggleSwitch(string text)
    {
        Text = text;
        AutoSize = true;
        Cursor = Cursors.Hand;
        Margin = new Padding(2, 6, 2, 6);
    }

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => isChecked;
        set
        {
            if (isChecked != value)
            {
                isChecked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding);
        return new Size((int)Math.Ceiling(Px(TrackWidth + LabelGap)) + text.Width, (int)Math.Ceiling(Math.Max(Px(TrackHeight + 4), text.Height)));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.UseHighQuality();
        var track = new RectangleF(0, (Height - Px(TrackHeight)) / 2F, Px(TrackWidth), Px(TrackHeight));
        var radius = track.Height / 2F;
        var knobRadius = Px(IsPressed ? 7 : IsHovered ? 7 : 6);

        if (isChecked)
        {
            graphics.FillRounded(!Enabled ? Colors.TextDisabled : IsHovered ? Colors.AccentHover : Colors.Accent, track, radius);
            graphics.FillCircle(Colors.OnAccent, new PointF(track.Right - radius, track.Y + radius), knobRadius);
        }
        else
        {
            graphics.FillRounded(IsHovered && Enabled ? Colors.SubtleHover : Colors.SubtlePressed, track, radius);
            var outline = Enabled ? Colors.ControlStrong : Colors.TextDisabled;
            graphics.DrawRounded(outline, track, radius);
            graphics.FillCircle(outline, new PointF(track.X + radius, track.Y + radius), knobRadius - Px(1));
        }

        var textLeft = (int)Math.Ceiling(Px(TrackWidth + LabelGap));
        graphics.DrawText(Text, Font, Enabled ? Colors.Text : Colors.TextDisabled, new Rectangle(textLeft, 0, Width - textLeft, Height), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Checked = !Checked;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode == Keys.Space)
        {
            Checked = !Checked;
            e.Handled = true;
        }
    }
}
