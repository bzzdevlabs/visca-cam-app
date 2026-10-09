using System;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Presentation;

/// <summary>Camera motion: pan/tilt, zoom, focus and home.</summary>
public sealed partial class MainPresenter
{
    private void SubscribeCameraEvents()
    {
        view.PanTiltRequested += (_, e) => WithCamera(ptz => ptz.Move(Orient(e.Pan, settings.InvertPan), Orient(e.Tilt, settings.InvertTilt), view.Speed));
        view.ZoomRequested += (_, e) => WithCamera(ptz => ptz.Zoom(e.Direction, view.Speed));
        view.FocusRequested += (_, e) => WithCamera(ptz => ptz.Focus(e.Direction));
        view.AutoFocusRequested += (_, e) => WithCamera(ptz => ptz.SetAutoFocus(e.Enabled));
        view.HomeRequested += (_, _) => WithCamera(ptz => ptz.Home());
    }

    private static Motion Orient(Motion motion, bool inverted) => inverted ? (Motion)(-(int)motion) : motion;

    /// <summary>Runs <paramref name="action"/> against the connected camera, reporting failures in the status bar.</summary>
    private bool WithCamera(Action<IPtzController> action)
    {
        if (session is null)
        {
            view.ShowStatus("Not connected. Press Connect first.", true);
            return false;
        }

        try
        {
            action(session.Ptz);
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            view.ShowStatus(ex.Message, true);
            return false;
        }
    }
}
