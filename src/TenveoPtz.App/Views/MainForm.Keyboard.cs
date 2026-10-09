using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TenveoPtz.App.Views.Controls;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.App.Views;

/// <summary>
/// Keyboard shortcuts: arrows pan/tilt (hold, combinable for diagonals), +/- or Page Up/Down zoom,
/// Home recentres, 1..9 recall presets. Ignored while an input field or list has focus.
/// </summary>
internal sealed partial class MainForm
{
    private readonly HashSet<Keys> heldArrows = new();
    private Motion heldZoom = Motion.None;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (IsKeyboardTarget(FocusedLeaf()) || (keyData & (Keys.Control | Keys.Alt)) != 0)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        var key = keyData & Keys.KeyCode;
        if (IsArrow(key))
        {
            if (heldArrows.Add(key))
            {
                RaisePanTiltFromArrows();
            }

            return true;
        }

        var zoom = ZoomFor(key);
        if (zoom != Motion.None)
        {
            if (heldZoom != zoom)
            {
                heldZoom = zoom;
                ZoomRequested?.Invoke(this, new MotionEventArgs(zoom));
            }

            return true;
        }

        if (key == Keys.Home)
        {
            HomeRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        var presetIndex = PresetIndexFor(key);
        if (presetIndex >= 0)
        {
            RecallPresetAtRequested?.Invoke(this, new PresetIndexEventArgs(presetIndex));
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (heldArrows.Remove(e.KeyCode))
        {
            RaisePanTiltFromArrows();
            e.Handled = true;
        }
        else if (heldZoom != Motion.None && ZoomFor(e.KeyCode) == heldZoom)
        {
            StopKeyboardZoom();
            e.Handled = true;
        }
    }

    /// <summary>Stops keyboard-driven motion when the window loses focus, so a key release is never missed.</summary>
    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
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

    /// <summary>Controls that need the arrow and digit keys themselves.</summary>
    private static bool IsKeyboardTarget(Control? control) =>
        control is TextBoxBase || control is ComboBox || control is UpDownBase || control is ListBox || control is FluentSlider;

    /// <summary>The focused control, looking inside nested containers (ActiveControl stops at the form's direct child).</summary>
    private Control? FocusedLeaf()
    {
        Control? control = ActiveControl;
        while (control is ContainerControl { ActiveControl: { } inner })
        {
            control = inner;
        }

        return control;
    }

    private static bool IsArrow(Keys key) => key is Keys.Left or Keys.Right or Keys.Up or Keys.Down;

    private static Motion ZoomFor(Keys key) => key switch
    {
        Keys.Add or Keys.Oemplus or Keys.PageUp => Motion.Positive,
        Keys.Subtract or Keys.OemMinus or Keys.PageDown => Motion.Negative,
        _ => Motion.None,
    };

    private static int PresetIndexFor(Keys key)
    {
        if (key >= Keys.D1 && key <= Keys.D9)
        {
            return key - Keys.D1;
        }

        if (key >= Keys.NumPad1 && key <= Keys.NumPad9)
        {
            return key - Keys.NumPad1;
        }

        return -1;
    }

    private static Motion Axis(bool negative, bool positive) =>
        negative == positive ? Motion.None : (positive ? Motion.Positive : Motion.Negative);

    private void RaisePanTiltFromArrows()
    {
        var pan = Axis(heldArrows.Contains(Keys.Left), heldArrows.Contains(Keys.Right));
        var tilt = Axis(heldArrows.Contains(Keys.Down), heldArrows.Contains(Keys.Up));
        PanTiltRequested?.Invoke(this, new PanTiltEventArgs(pan, tilt));
    }

    private void StopKeyboardZoom()
    {
        heldZoom = Motion.None;
        ZoomRequested?.Invoke(this, new MotionEventArgs(Motion.None));
    }
}
