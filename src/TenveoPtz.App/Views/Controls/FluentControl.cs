using System;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>
/// Base of the custom-drawn Fluent controls: double-buffered painting, hover/pressed tracking,
/// DPI scaling helper and automatic repaint when the theme changes.
/// The background colour is inherited from the parent (ambient), so corners blend in.
/// </summary>
internal abstract class FluentControl : Control
{
    protected FluentControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor,
            true);
        Font = FluentFonts.Body;
        Theme.Changed += OnThemeChanged;
    }

    protected static Palette Colors => Theme.Current;

    protected bool IsHovered { get; private set; }

    protected bool IsPressed { get; private set; }

    /// <summary>Converts logical (96 DPI) pixels to device pixels.</summary>
    protected float Px(float logicalPixels) => logicalPixels * DeviceDpi / 96F;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        IsHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        IsHovered = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            IsPressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
        {
            IsPressed = false;
            Invalidate();
        }
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        if (IsPressed)
        {
            IsPressed = false;
            Invalidate();
        }
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected virtual void OnThemeChanged() => Invalidate();

    private void OnThemeChanged(object? sender, EventArgs e) => OnThemeChanged();
}
