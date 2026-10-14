namespace TenveoPtz.Core.Ptz;

/// <summary>Access to the camera axes exposed by the video driver (UVC camera control).</summary>
public interface ICameraAxes
{
    /// <summary>Returns the axis range, or <c>null</c> when the camera does not support the axis.</summary>
    AxisRange? GetRange(CameraAxis axis);

    /// <summary>Returns the current axis value, or <c>null</c> when it cannot be read.</summary>
    int? GetValue(CameraAxis axis);

    void SetValue(CameraAxis axis, int value);

    void SetAutomatic(CameraAxis axis, bool enabled);

    /// <summary>
    /// Speed range of the axis's continuous (relative) motion, from the slowest to the fastest
    /// speed, or <c>null</c> when the camera only supports absolute positioning on that axis.
    /// </summary>
    AxisRange? GetSpeedRange(CameraAxis axis);

    /// <summary>Moves the axis continuously: the sign gives the direction, the magnitude the speed; 0 stops.</summary>
    void SetSpeed(CameraAxis axis, int speed);
}
