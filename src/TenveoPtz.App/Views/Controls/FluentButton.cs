using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>
/// Fluent button with an optional icon. Besides <see cref="Control.Click"/> it reports
/// <see cref="Pressed"/>/<see cref="Released"/> for "move while held" camera controls.
/// </summary>
internal sealed class FluentButton : FluentControl
{
    private const float IconSize = 16F;
    private const float IconGap = 8F;
    private const float MinHeight = 32F;

    private string? glyph;
    private ButtonAppearance appearance;
    private bool isChecked;
    private bool held;

    public FluentButton(string text, string? glyph = null, ButtonAppearance appearance = ButtonAppearance.Standard)
    {
        Text = text;
        this.glyph = glyph;
        this.appearance = appearance;
        AutoSize = true;
        Cursor = Cursors.Hand;
        Margin = new Padding(2);
        Padding = new Padding(text.Length == 0 ? 8 : 12, 5, text.Length == 0 ? 8 : 12, 6);
    }

    public event EventHandler? Pressed;

    public event EventHandler? Released;

    public string? Glyph
    {
        get => glyph;
        set
        {
            glyph = value;
            Invalidate();
        }
    }

    public ButtonAppearance Appearance
    {
        get => appearance;
        set
        {
            appearance = value;
            Invalidate();
        }
    }

    /// <summary>Toggle state, shown by subtle buttons (e.g. "always on top").</summary>
    public bool IsChecked
    {
        get => isChecked;
        set
        {
            isChecked = value;
            Invalidate();
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var width = 0F;
        if (glyph is not null)
        {
            width += Px(IconSize);
        }

        if (Text.Length > 0)
        {
            width += TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            if (glyph is not null)
            {
                width += Px(IconGap);
            }
        }

        var height = Math.Max(Px(MinHeight), Font.Height + Padding.Vertical);
        return new Size((int)Math.Ceiling(width) + Padding.Horizontal, (int)Math.Ceiling(height));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.UseHighQuality();
        var bounds = new RectangleF(0, 0, Width, Height);
        var state = new ButtonChrome.VisualState(Enabled, IsHovered, IsPressed, isChecked);
        var foreground = ButtonChrome.Paint(graphics, bounds, DeviceDpi / 96F, appearance, state);

        var textWidth = Text.Length > 0 ? TextRenderer.MeasureText(graphics, Text, Font, Size.Empty, TextFormatFlags.NoPadding).Width : 0;
        var contentWidth = textWidth + (glyph is null ? 0 : Px(IconSize) + (textWidth > 0 ? Px(IconGap) : 0));
        var x = (Width - contentWidth) / 2F;
        if (glyph is not null)
        {
            graphics.DrawGlyph(glyph, FluentFonts.Icon(IconSize), foreground, new PointF(x + (Px(IconSize) / 2F), Height / 2F));
            x += Px(IconSize) + Px(IconGap);
        }

        if (textWidth > 0)
        {
            graphics.DrawText(Text, Font, foreground, new Rectangle((int)x, 0, textWidth + 1, Height), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        if (Focused && ShowFocusCues)
        {
            ButtonChrome.PaintFocus(graphics, bounds, DeviceDpi / 96F);
        }
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && !held && Enabled)
        {
            held = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        Release();
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        Release();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Release();
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void Release()
    {
        if (held)
        {
            held = false;
            Released?.Invoke(this, EventArgs.Empty);
        }
    }
}
