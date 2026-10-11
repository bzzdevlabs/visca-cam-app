using System;
using AppKit;

namespace TenveoPtz.Mac.Views.Controls;

/// <summary>Button acting while held down (zoom, focus): raises <see cref="Pressed"/> then <see cref="Released"/>.</summary>
internal sealed class HoldButton : NSButton
{
    public HoldButton()
    {
        BezelStyle = NSBezelStyle.Rounded;
        RefusesFirstResponder = true;
    }

    public event EventHandler? Pressed;

    public event EventHandler? Released;

    public override bool AcceptsFirstMouse(NSEvent? theEvent) => true;

    public override void MouseDown(NSEvent theEvent)
    {
        if (!Enabled)
        {
            base.MouseDown(theEvent);
            return;
        }

        Pressed?.Invoke(this, EventArgs.Empty);
        try
        {
            // Tracks the mouse (highlighting the button) and returns once it is released.
            base.MouseDown(theEvent);
        }
        finally
        {
            Released?.Invoke(this, EventArgs.Empty);
        }
    }
}
