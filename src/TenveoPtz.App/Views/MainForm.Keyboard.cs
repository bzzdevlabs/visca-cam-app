using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TenveoPtz.App.Infrastructure.Input;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.App.Views;

/// <summary>
/// Keyboard shortcuts: arrows pan/tilt (hold, combinable for diagonals), +/- or Page Up/Down zoom,
/// Home recentres, 1..9 recall presets (all ignored while a text field has focus, so the camera
/// keys keep working after clicking the speed slider or the preset list). Key auto-repeat is ignored:
/// holding a key sends one move on press and one stop on release, so the motion stays continuous.
/// Holding Shift switches to precise (slow) moves. Ctrl+, opens the settings. Ctrl+Shift+D toggles
/// the hidden debug console.
/// With "Numpad in background" on, the numeric keypad keys do the same while another application
/// (Zoom, Teams...) is in front.
/// </summary>
internal sealed partial class MainForm
{
    private readonly HashSet<Keys> heldArrows = new();

    /// <summary>Keypad keys pressed while another application was in front, until released.</summary>
    private readonly HashSet<Keys> heldInBackground = new();
    private Motion heldZoom = Motion.None;
    private bool shiftHeld;
    private NumpadHook? numpadHook;
    private bool isConnected;
    private bool backgroundNumpad;
    private AppTheme theme;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == DebugConsoleKeys)
        {
            ToggleDebugConsole();
            return true;
        }

        if (keyData == (Keys.Control | Keys.Oemcomma))
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if ((keyData & Keys.KeyCode) == Keys.ShiftKey)
        {
            SetShiftHeld(true);
            return base.ProcessCmdKey(ref msg, keyData);
        }

        if (IsKeyboardTarget(FocusedLeaf()) || (keyData & (Keys.Control | Keys.Alt)) != 0)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        return HandleCameraKeyDown(keyData & Keys.KeyCode) || base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode == Keys.ShiftKey)
        {
            SetShiftHeld(false);
        }
        else if (HandleCameraKeyUp(e.KeyCode))
        {
            e.Handled = true;
        }
    }

    /// <summary>Stops keyboard-driven motion when the window loses focus, so a key release is never missed.</summary>
    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        SetShiftHeld(false);
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

    /// <summary>Starts or stops watching the numeric keypad while other applications are in front.</summary>
    private void UpdateNumpadHook()
    {
        if (BackgroundNumpad && numpadHook is null && IsHandleCreated)
        {
            try
            {
                numpadHook = new NumpadHook(OnBackgroundKeyDown, OnBackgroundKeyUp);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                statusLine.Show(Strings.CannotWatchKeypad(ex.NativeErrorCode), true);
            }
        }
        else if (!BackgroundNumpad && numpadHook is not null)
        {
            numpadHook.Dispose();
            numpadHook = null;
            ReleaseBackgroundKeys();
        }
    }

    /// <summary>Keypad key pressed in another application: drives the camera when connected. Returns whether to swallow it.</summary>
    private bool OnBackgroundKeyDown(Keys key)
    {
        if (!isConnected)
        {
            return false;
        }

        // Auto-repeat sends the press again: act on the first one only, but keep swallowing.
        if (heldInBackground.Add(key))
        {
            BeginInvoke(new Action(() => HandleCameraKeyDown(key)));
        }

        return true;
    }

    private bool OnBackgroundKeyUp(Keys key)
    {
        if (!heldInBackground.Remove(key))
        {
            return false;
        }

        BeginInvoke(new Action(() => HandleCameraKeyUp(key)));
        return true;
    }

    private void ReleaseBackgroundKeys()
    {
        foreach (var key in new List<Keys>(heldInBackground))
        {
            HandleCameraKeyUp(key);
        }

        heldInBackground.Clear();
    }

    /// <summary>Camera action of a key press: pan/tilt, zoom, home or preset. Returns whether the key is a camera key.</summary>
    private bool HandleCameraKeyDown(Keys key)
    {
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

        return false;
    }

    /// <summary>Stops the move or zoom a key release ends. Returns whether it ended one.</summary>
    private bool HandleCameraKeyUp(Keys key)
    {
        if (heldArrows.Remove(key))
        {
            RaisePanTiltFromArrows();
            return true;
        }

        if (heldZoom != Motion.None && ZoomFor(key) == heldZoom)
        {
            StopKeyboardZoom();
            return true;
        }

        return false;
    }

    /// <summary>Controls where the arrow, digit and +/- keys are typing, not camera control.</summary>
    private static bool IsKeyboardTarget(Control? control) =>
        control is TextBoxBase || control is ComboBox || control is UpDownBase;

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

    private void SetShiftHeld(bool held)
    {
        if (shiftHeld != held)
        {
            shiftHeld = held;
            PreciseModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void StopKeyboardZoom()
    {
        heldZoom = Motion.None;
        ZoomRequested?.Invoke(this, new MotionEventArgs(Motion.None));
    }
}
