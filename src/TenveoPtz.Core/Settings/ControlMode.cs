namespace TenveoPtz.Core.Settings;

/// <summary>How pan/tilt/zoom commands reach the camera.</summary>
public enum ControlMode
{
    /// <summary>VISCA protocol over a serial (RS-232/RS-485) COM port.</summary>
    Visca,

    /// <summary>UVC camera controls over the USB video connection.</summary>
    Uvc,
}
