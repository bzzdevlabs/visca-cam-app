using System;
using System.Runtime.InteropServices;
using AppKit;
using CoreGraphics;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Mac.Views.Controls;

/// <summary>
/// Circular pan/tilt pad with eight directions (hold to move) and a home button in the centre.
/// </summary>
internal sealed class DirectionPad : NSView
{
    private const int Centre = -1;
    private const int Nothing = -2;
    private const double InnerRatio = 0.36;
    private const double Size = 144;

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
        TranslatesAutoresizingMaskIntoConstraints = false;
        WidthAnchor.ConstraintEqualTo((NFloat)Size).Active = true;
        HeightAnchor.ConstraintEqualTo((NFloat)Size).Active = true;
        ToolTip = Strings.PadTip;
    }

    public event EventHandler<PanTiltEventArgs>? PanTiltRequested;

    public event EventHandler? HomeRequested;

    private CGPoint Middle => new(Bounds.Width / 2, Bounds.Height / 2);

    private double OuterRadius => (Math.Min(Bounds.Width, Bounds.Height) / 2) - 1;

    private double InnerRadius => OuterRadius * InnerRatio;

    public override bool AcceptsFirstMouse(NSEvent? theEvent) => true;

    public override void DrawRect(CGRect dirtyRect)
    {
        var middle = Middle;
        var outer = OuterRadius;
        var inner = InnerRadius;

        NSColor.ControlBackground.SetFill();
        Circle(middle, outer).Fill();
        var highlighted = pressed >= 0 ? pressed : hovered;
        if (highlighted >= 0)
        {
            (pressed >= 0 ? NSColor.ControlAccent : NSColor.QuaternaryLabel).SetFill();
            var sector = new NSBezierPath();
            sector.MoveTo(middle);
            sector.AppendPathWithArc(middle, (NFloat)outer, (NFloat)((highlighted * 45) - 22.5), (NFloat)((highlighted * 45) + 22.5));
            sector.ClosePath();
            sector.Fill();
        }

        NSColor.Separator.SetStroke();
        Circle(middle, outer).Stroke();

        // Chevrons for the four axes, small dots for the diagonals.
        var ring = (outer + inner) / 2;
        for (var index = 0; index < Sectors.Length; index++)
        {
            var angle = index * 45 * Math.PI / 180;
            var position = new CGPoint(middle.X + (Math.Cos(angle) * ring), middle.Y + (Math.Sin(angle) * ring));
            var color = index == pressed ? NSColor.White : NSColor.Label;
            if (index % 2 == 0)
            {
                color.SetStroke();
                Chevron(position, angle).Stroke();
            }
            else
            {
                color.SetFill();
                Circle(position, 2).Fill();
            }
        }

        (pressed == Centre ? NSColor.QuaternaryLabel : hovered == Centre ? NSColor.UnemphasizedSelectedContentBackground : NSColor.WindowBackground).SetFill();
        Circle(middle, inner).Fill();
        NSColor.Separator.SetStroke();
        Circle(middle, inner).Stroke();
        NSColor.Label.SetStroke();
        House(middle).Stroke();
    }

    public override void UpdateTrackingAreas()
    {
        base.UpdateTrackingAreas();
        foreach (var area in TrackingAreas())
        {
            RemoveTrackingArea(area);
        }

        AddTrackingArea(new NSTrackingArea(
            Bounds,
            NSTrackingAreaOptions.MouseMoved | NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.InVisibleRect,
            this,
            null));
    }

    public override void MouseMoved(NSEvent theEvent) => SetHovered(SectorAt(theEvent));

    public override void MouseExited(NSEvent theEvent) => SetHovered(Nothing);

    public override void MouseDown(NSEvent theEvent)
    {
        pressed = SectorAt(theEvent);
        if (pressed >= 0)
        {
            var (pan, tilt) = Sectors[pressed];
            PanTiltRequested?.Invoke(this, new PanTiltEventArgs(pan, tilt));
        }

        NeedsDisplay = true;
    }

    public override void MouseUp(NSEvent theEvent)
    {
        if (pressed == Centre && SectorAt(theEvent) == Centre)
        {
            HomeRequested?.Invoke(this, EventArgs.Empty);
        }

        if (pressed >= 0)
        {
            PanTiltRequested?.Invoke(this, new PanTiltEventArgs(Motion.None, Motion.None));
        }

        pressed = Nothing;
        NeedsDisplay = true;
    }

    private static NSBezierPath Circle(CGPoint centre, double radius) =>
        NSBezierPath.FromOvalInRect(new CGRect(centre.X - radius, centre.Y - radius, radius * 2, radius * 2));

    /// <summary>"&gt;" pointing at <paramref name="angle"/> (radians, counter-clockwise from the right).</summary>
    private static NSBezierPath Chevron(CGPoint position, double angle)
    {
        CGPoint Point(double along, double across) => new(
            position.X + (along * Math.Cos(angle)) - (across * Math.Sin(angle)),
            position.Y + (along * Math.Sin(angle)) + (across * Math.Cos(angle)));

        var path = new NSBezierPath { LineWidth = 2, LineCapStyle = NSLineCapStyle.Round, LineJoinStyle = NSLineJoinStyle.Round };
        path.MoveTo(Point(-3, 6));
        path.LineTo(Point(3, 0));
        path.LineTo(Point(-3, -6));
        return path;
    }

    private static NSBezierPath House(CGPoint centre)
    {
        var path = new NSBezierPath { LineWidth = 1.5f, LineJoinStyle = NSLineJoinStyle.Round };
        var x = centre.X;
        var y = centre.Y;
        path.MoveTo(new CGPoint(x - 8, y - 1));
        path.LineTo(new CGPoint(x, y + 7));
        path.LineTo(new CGPoint(x + 8, y - 1));
        path.MoveTo(new CGPoint(x - 6, y + 1));
        path.LineTo(new CGPoint(x - 6, y - 7));
        path.LineTo(new CGPoint(x + 6, y - 7));
        path.LineTo(new CGPoint(x + 6, y + 1));
        return path;
    }

    private void SetHovered(int value)
    {
        if (hovered != value)
        {
            hovered = value;
            NeedsDisplay = true;
        }
    }

    private int SectorAt(NSEvent theEvent)
    {
        var point = ConvertPointFromView(theEvent.LocationInWindow, null);
        var dx = point.X - Middle.X;
        var dy = point.Y - Middle.Y;
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
