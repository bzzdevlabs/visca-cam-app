using System;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Presentation;

/// <summary>Camera motion: pan/tilt, zoom, focus and home, plus raw protocol commands from the debug console.</summary>
public sealed partial class MainPresenter
{
    private Motion heldPan;
    private Motion heldTilt;
    private Motion heldZoom;

    private void SubscribeCameraEvents()
    {
        view.PanTiltRequested += (_, e) => PanTilt(e.Pan, e.Tilt);
        view.ZoomRequested += (_, e) => Zoom(e.Direction);
        view.SpeedChanged += (_, _) => ApplySpeed();
        view.FocusRequested += (_, e) => Drive($"Focus {e.Direction}", ptz => ptz.Focus(e.Direction));
        view.AutoFocusRequested += (_, e) => Drive($"Autofocus {(e.Enabled ? "on" : "off")}", ptz => ptz.SetAutoFocus(e.Enabled));
        view.HomeRequested += (_, _) => Drive("Home", ptz => ptz.Home());
        view.RawCommandRequested += (_, e) => SendRawCommand(e.Text);
    }

    private void SendRawCommand(string text)
    {
        var bytes = HexBytes.TryParse(text);
        if (bytes is null)
        {
            ShowStatus($"\"{text}\" is not a list of hexadecimal bytes, e.g. 81 01 06 04 FF.", true);
            return;
        }

        if (session is not null && session.Ptz is not IRawCommandSender)
        {
            ShowStatus("Raw commands need a VISCA connection.", true);
            return;
        }

        if (WithCamera(ptz => ((IRawCommandSender)ptz).SendRaw(bytes)))
        {
            ShowStatus("Sent raw command " + HexBytes.Format(bytes) + ".", false);
        }
    }

    private void PanTilt(Motion pan, Motion tilt)
    {
        heldPan = pan;
        heldTilt = tilt;
        Drive($"Pan {pan}, tilt {tilt}", ptz => ptz.Move(Orient(pan, settings.InvertPan), Orient(tilt, settings.InvertTilt), view.Speed));
    }

    private void Zoom(Motion direction)
    {
        heldZoom = direction;
        Drive($"Zoom {direction}", ptz => ptz.Zoom(direction, view.Speed));
    }

    /// <summary>Moving the speed slider while a move is held takes effect immediately.</summary>
    private void ApplySpeed()
    {
        if (session is null)
        {
            return;
        }

        if (heldPan != Motion.None || heldTilt != Motion.None)
        {
            PanTilt(heldPan, heldTilt);
        }

        if (heldZoom != Motion.None)
        {
            Zoom(heldZoom);
        }
    }

    private void Drive(string intent, Action<IPtzController> action)
    {
        log.Debug($"{intent} (speed {view.Speed:0.00})");
        WithCamera(action);
    }

    /// <summary>
    /// Maps a pad direction onto the camera axis. Tenveo cameras move the opposite way to the
    /// VISCA/UVC conventions, so directions are reversed by default; the invert settings undo that,
    /// e.g. for a camera mounted upside down.
    /// </summary>
    private static Motion Orient(Motion motion, bool inverted) => inverted ? motion : (Motion)(-(int)motion);

    /// <summary>Runs <paramref name="action"/> against the connected camera, reporting failures in the status bar.</summary>
    private bool WithCamera(Action<IPtzController> action)
    {
        if (session is null)
        {
            ShowStatus("Not connected. Press Connect first.", true);
            return false;
        }

        try
        {
            action(session.Ptz);
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            ShowStatus(ex.Message, true, ex);
            return false;
        }
    }
}
