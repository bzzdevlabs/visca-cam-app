using System;
using System.Collections.Generic;
using AppKit;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Mac.Views;

/// <summary>
/// Keyboard shortcuts: arrows pan/tilt (hold, combinable for diagonals), +/- or Page Up/Down zoom,
/// Home recentres, 1..9 recall presets. The window only receives keys that the focused control
/// does not use itself, so text fields, the preset list and the slider keep their own keys.
/// </summary>
internal sealed partial class MainWindow
{
    private const ushort LeftArrow = 123;
    private const ushort RightArrow = 124;
    private const ushort DownArrow = 125;
    private const ushort UpArrow = 126;
    private const ushort HomeKey = 115;
    private const ushort PageUpKey = 116;
    private const ushort PageDownKey = 121;

    private readonly HashSet<ushort> heldArrows = new();
    private Motion heldZoom = Motion.None;

    public override void KeyDown(NSEvent theEvent)
    {
        if (!HandleKeyDown(theEvent))
        {
            base.KeyDown(theEvent);
        }
    }

    public override void KeyUp(NSEvent theEvent)
    {
        var key = theEvent.KeyCode;
        if (heldArrows.Remove(key))
        {
            RaisePanTiltFromArrows();
        }
        else if (heldZoom != Motion.None && ZoomFor(theEvent) == heldZoom)
        {
            StopKeyboardZoom();
        }
        else
        {
            base.KeyUp(theEvent);
        }
    }

    /// <summary>Stops keyboard-driven motion when the window loses focus, so a key release is never missed.</summary>
    public override void ResignKeyWindow()
    {
        base.ResignKeyWindow();
        if (heldArrows.Count > 0)
        {
            heldArrows.Clear();
            RaisePanTiltFromArrows();
        }

        if (heldZoom != Motion.None)
        {
            StopKeyboardZoom();
        }
    }

    private static bool IsArrow(ushort key) => key is LeftArrow or RightArrow or DownArrow or UpArrow;

    private static Motion ZoomFor(NSEvent theEvent)
    {
        if (theEvent.KeyCode == PageUpKey)
        {
            return Motion.Positive;
        }

        if (theEvent.KeyCode == PageDownKey)
        {
            return Motion.Negative;
        }

        return theEvent.CharactersIgnoringModifiers switch
        {
            "+" or "=" => Motion.Positive,
            "-" or "_" => Motion.Negative,
            _ => Motion.None,
        };
    }

    private static int PresetIndexFor(NSEvent theEvent) =>
        theEvent.CharactersIgnoringModifiers is { Length: 1 } text && text[0] >= '1' && text[0] <= '9' ? text[0] - '1' : -1;

    private static Motion Axis(bool negative, bool positive) =>
        negative == positive ? Motion.None : (positive ? Motion.Positive : Motion.Negative);

    private bool HandleKeyDown(NSEvent theEvent)
    {
        if ((theEvent.ModifierFlags & (NSEventModifierMask.CommandKeyMask | NSEventModifierMask.ControlKeyMask | NSEventModifierMask.AlternateKeyMask)) != 0)
        {
            return false;
        }

        var key = theEvent.KeyCode;
        if (IsArrow(key))
        {
            if (heldArrows.Add(key))
            {
                RaisePanTiltFromArrows();
            }

            return true;
        }

        var zoom = ZoomFor(theEvent);
        if (zoom != Motion.None)
        {
            if (heldZoom != zoom)
            {
                heldZoom = zoom;
                RaiseZoom(zoom);
            }

            return true;
        }

        if (theEvent.IsARepeat)
        {
            return true;
        }

        if (key == HomeKey)
        {
            Safely(() => HomeRequested?.Invoke(this, EventArgs.Empty));
            return true;
        }

        var presetIndex = PresetIndexFor(theEvent);
        if (presetIndex >= 0)
        {
            Safely(() => RecallPresetAtRequested?.Invoke(this, new PresetIndexEventArgs(presetIndex)));
            return true;
        }

        return false;
    }

    private void RaisePanTiltFromArrows()
    {
        var pan = Axis(heldArrows.Contains(LeftArrow), heldArrows.Contains(RightArrow));
        var tilt = Axis(heldArrows.Contains(DownArrow), heldArrows.Contains(UpArrow));
        Safely(() => PanTiltRequested?.Invoke(this, new PanTiltEventArgs(pan, tilt)));
    }

    private void StopKeyboardZoom()
    {
        heldZoom = Motion.None;
        RaiseZoom(Motion.None);
    }
}
