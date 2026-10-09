namespace TenveoPtz.Core.Ptz;

/// <summary>Absolute access to the camera axes exposed by the video driver (UVC camera control).</summary>
public interface ICameraAxes
{
    /// <summary>Returns the axis range, or <c>null</c> when the camera does not support the axis.</summary>
    AxisRange? GetRange(CameraAxis axis);

    /// <summary>Returns the current axis value, or <c>null</c> when it cannot be read.</summary>
    int? GetValue(CameraAxis axis);

    void SetValue(CameraAxis axis, int value);

    void SetAutomatic(CameraAxis axis, bool enabled);
}
