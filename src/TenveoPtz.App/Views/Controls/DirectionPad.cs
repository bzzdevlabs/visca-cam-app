using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.App.Views.Controls;

/// <summary>
/// Circular pan/tilt pad with eight directions (hold to move) and a home button in the centre.
/// </summary>
internal sealed class DirectionPad : FluentControl
{
    private const int Centre = -1;
    private const int Nothing = -2;
    private const float InnerRatio = 0.36F;

    // Sectors counter-clockwise from "right", 45 degrees each.
    private static readonly (Motion Pan, Motion Tilt)[] Sectors =
    {
        (Motion.Positive, Motion.None),
        (Motion.Positive, Motion.Positive),
        (Motion.None, Motion.Positive),
        (Motion.Negative, Motion.Positive),
        (Motion.Negative, Motion.None),
        (Motion.Negative, Motion.Negative),
        (Motion.None, Motion.Negative),
        (Motion.Positive, Motion.Negative),
    };

    private int hovered = Nothing;
    private int pressed = Nothing;

    public DirectionPad()
    {
        Size = new Size(144, 144);
        TabStop = false;
        Cursor = Cursors.Hand;
    }

    public event EventHandler<PanTiltEventArgs>? PanTiltRequested;

    public event EventHandler? HomeRequested;

    private PointF Middle => new(Width / 2F, Height / 2F);

    private float OuterRadius => (Math.Min(Width, Height) / 2F) - 1;

    private float InnerRadius => OuterRadius * InnerRatio;

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.UseHighQuality();
        var middle = Middle;
        var outer = OuterRadius;
        var inner = InnerRadius;

        graphics.FillCircle(Colors.ControlFill, middle, outer);
        var highlighted = pressed >= 0 ? pressed : hovered;
        if (highlighted >= 0)
        {
            using var brush = new SolidBrush(pressed >= 0 ? Colors.Accent : Colors.ControlFillHover);
            graphics.FillPie(brush, middle.X - outer, middle.Y - outer, outer * 2, outer * 2, (-highlighted * 45F) - 22.5F, 45F);
        }

        graphics.DrawCircle(Colors.ControlBorder, middle, outer);

        // Chevrons for the four axes, small dots for the diagonals (a rotated chevron reads as a corner).
        var ring = (outer + inner) / 2F;
        for (var sector = 0; sector < Sectors.Length; sector++)
        {
            var angle = sector * 45.0;
            var position = new PointF(
                middle.X + (float)(Math.Cos(angle * Math.PI / 180) * ring),
                middle.Y - (float)(Math.Sin(angle * Math.PI / 180) * ring));
            var color = sector == pressed ? Colors.OnAccent : Enabled ? Colors.Text : Colors.TextDisabled;
            if (sector % 2 == 0)
            {
                graphics.DrawGlyph(Glyphs.ChevronRight, FluentFonts.Icon(14), color, position, (float)-angle);
            }
            else
            {
                graphics.FillCircle(color, position, Px(2));
            }
        }

        var centreFill = pressed == Centre ? Colors.ControlFillPressed : hovered == Centre ? Colors.ControlFillHover : Colors.Surface;
        graphics.FillCircle(centreFill, middle, inner);
        graphics.DrawCircle(Colors.ControlBorder, middle, inner);
        graphics.DrawGlyph(Glyphs.Home, FluentFonts.Icon(16), Enabled ? Colors.Text : Colors.TextDisabled, middle);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        SetHovered(HitTest(e.Location));
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        SetHovered(Nothing);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !Enabled)
        {
            return;
        }

        pressed = HitTest(e.Location);
        if (pressed >= 0)
        {
            var (pan, tilt) = Sectors[pressed];
            PanTiltRequested?.Invoke(this, new PanTiltEventArgs(pan, tilt));
        }

        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (pressed == Centre && HitTest(e.Location) == Centre)
        {
            HomeRequested?.Invoke(this, EventArgs.Empty);
        }

        base.OnMouseUp(e);
        Release();
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        Release();
    }

    private void Release()
    {
        if (pressed >= 0)
        {
            PanTiltRequested?.Invoke(this, new PanTiltEventArgs(Motion.None, Motion.None));
        }

        if (pressed != Nothing)
        {
            pressed = Nothing;
            Invalidate();
        }
    }

    private void SetHovered(int value)
    {
        if (hovered != value)
        {
            hovered = value;
            Invalidate();
        }
    }

    private int HitTest(Point point)
    {
        var dx = point.X - Middle.X;
        var dy = Middle.Y - point.Y;
        var distance = Math.Sqrt((dx * dx) + (dy * dy));
        if (distance > OuterRadius)
        {
            return Nothing;
        }

        if (distance <= InnerRadius)
        {
            return Centre;
        }

        var angle = (Math.Atan2(dy, dx) * 180 / Math.PI) + 360 + 22.5;
        return (int)(angle % 360 / 45);
    }
}
