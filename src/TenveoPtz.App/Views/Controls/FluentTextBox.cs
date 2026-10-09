using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Native text box wrapped in a Fluent frame with an accent underline while focused.</summary>
internal sealed class FluentTextBox : FluentControl
{
    private const float CornerRadius = 4F;

    private readonly TextBox input;

    public FluentTextBox()
    {
        input = new TextBox { BorderStyle = BorderStyle.None, Font = FluentFonts.Body };
        input.GotFocus += (_, _) => Invalidate();
        input.LostFocus += (_, _) => Invalidate();
        Controls.Add(input);
        Size = new Size(320, 32);
        ApplyTheme();
    }

    public string Value
    {
        get => input.Text;
        set => input.Text = value;
    }

    public void SelectAll()
    {
        input.Focus();
        input.SelectAll();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        var inset = (int)Px(10);
        input.SetBounds(inset, (Height - input.Height) / 2, Width - (2 * inset), input.Height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.UseHighQuality();
        var bounds = new RectangleF(0, 0, Width, Height);
        graphics.FillRounded(input.BackColor, bounds, Px(CornerRadius));
        graphics.DrawRounded(Colors.ControlBorder, bounds, Px(CornerRadius));
        var line = input.Focused ? Px(2) : Px(1);
        using var brush = new SolidBrush(input.Focused ? Colors.Accent : Colors.ControlStrong);
        graphics.FillRectangle(brush, Px(CornerRadius), Height - line, Width - Px(2 * CornerRadius), line);
    }

    protected override void OnThemeChanged()
    {
        ApplyTheme();
        base.OnThemeChanged();
    }

    private void ApplyTheme()
    {
        input.BackColor = Colors.IsDark ? Color.FromArgb(0x2D, 0x2D, 0x2D) : Color.White;
        input.ForeColor = Colors.Text;
    }
}
