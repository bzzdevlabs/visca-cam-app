using System;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Presentation;

/// <summary>Camera motion: pan/tilt, zoom, focus and home, plus raw protocol commands from the debug console.</summary>
public sealed partial class MainPresenter
{
    private void SubscribeCameraEvents()
    {
        view.PanTiltRequested += (_, e) => Drive($"Pan {e.Pan}, tilt {e.Tilt}", ptz => ptz.Move(Orient(e.Pan, settings.InvertPan), Orient(e.Tilt, settings.InvertTilt), view.Speed));
        view.ZoomRequested += (_, e) => Drive($"Zoom {e.Direction}", ptz => ptz.Zoom(e.Direction, view.Speed));
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

    private void Drive(string intent, Action<IPtzController> action)
    {
        log.Debug($"{intent} (speed {view.Speed:0.00})");
        WithCamera(action);
    }

    private static Motion Orient(Motion motion, bool inverted) => inverted ? (Motion)(-(int)motion) : motion;

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
