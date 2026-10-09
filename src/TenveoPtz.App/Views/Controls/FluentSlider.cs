using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Windows 11 slider: accent-filled rail and a round thumb. Drag, use the mouse wheel or the keyboard.</summary>
internal sealed class FluentSlider : FluentControl
{
    private const float RailHeight = 4F;
    private const float ThumbRadius = 10F;

    private readonly int minimum;
    private readonly int maximum;
    private int value;

    public FluentSlider(int minimum, int maximum, int value)
    {
        this.minimum = minimum;
        this.maximum = maximum;
        this.value = value;
        Size = new Size(160, 32);
        Cursor = Cursors.Hand;
    }

    public event EventHandler? ValueChanged;

    public int Minimum => minimum;

    public int Maximum => maximum;

    public int Value
    {
        get => value;
        set
        {
            var clamped = Math.Max(minimum, Math.Min(maximum, value));
            if (clamped != this.value)
            {
                this.value = clamped;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private float RailLeft => Px(ThumbRadius);

    private float RailRight => Width - Px(ThumbRadius);

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.UseHighQuality();
        var middle = Height / 2F;
        var thumbX = RailLeft + ((RailRight - RailLeft) * (value - minimum) / Math.Max(1, maximum - minimum));
        var rail = Px(RailHeight);

        graphics.FillRounded(Colors.ControlStrong, new RectangleF(RailLeft, middle - (rail / 2), RailRight - RailLeft, rail), rail / 2);
        graphics.FillRounded(Enabled ? Colors.Accent : Colors.TextDisabled, new RectangleF(RailLeft, middle - (rail / 2), thumbX - RailLeft, rail), rail / 2);

        var thumb = new PointF(thumbX, middle);
        if (Focused && ShowFocusCues)
        {
            graphics.DrawCircle(Colors.Text, thumb, Px(ThumbRadius + 3), Px(2));
        }

        graphics.FillCircle(Colors.ControlFill, thumb, Px(ThumbRadius));
        graphics.DrawCircle(Colors.ControlBorder, thumb, Px(ThumbRadius));
        var inner = IsPressed ? 5F : IsHovered ? 7F : 6F;
        graphics.FillCircle(Enabled ? (IsHovered ? Colors.AccentHover : Colors.Accent) : Colors.TextDisabled, thumb, Px(inner));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            SetFromX(e.X);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (IsPressed)
        {
            SetFromX(e.X);
        }
    }

    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var step = Math.Max(1, (maximum - minimum) / 10);
        int? target = e.KeyCode switch
        {
            Keys.Left or Keys.Down => value - 1,
            Keys.Right or Keys.Up => value + 1,
            Keys.PageDown => value - step,
            Keys.PageUp => value + step,
            Keys.Home => minimum,
            Keys.End => maximum,
            _ => null,
        };
        if (target.HasValue)
        {
            Value = target.Value;
            e.Handled = true;
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        Value += Math.Sign(e.Delta);
    }

    private void SetFromX(int x)
    {
        var fraction = (x - RailLeft) / Math.Max(1F, RailRight - RailLeft);
        Value = minimum + (int)Math.Round(fraction * (maximum - minimum));
    }
}
