using System;
using TenveoPtz.App.Infrastructure.DirectShow.Interop;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.App.Infrastructure.DirectShow;

/// <summary>Adapts DirectShow's <see cref="IAMCameraControl"/> to <see cref="ICameraAxes"/>.</summary>
internal sealed class DirectShowCameraAxes : ICameraAxes
{
    private readonly IAMCameraControl control;

    public DirectShowCameraAxes(IAMCameraControl control)
    {
        this.control = control ?? throw new ArgumentNullException(nameof(control));
    }

    public AxisRange? GetRange(CameraAxis axis)
    {
        var supported = control.GetRange(ToProperty(axis), out var min, out var max, out var step, out var defaultValue, out _) == 0;
        return supported && max > min ? new AxisRange(min, max, step, defaultValue) : null;
    }

    public int? GetValue(CameraAxis axis) =>
        control.Get(ToProperty(axis), out var value, out _) == 0 ? value : null;

    public void SetValue(CameraAxis axis, int value) =>
        ComHelper.ThrowOnFailure(control.Set(ToProperty(axis), value, CameraControlFlags.Manual), $"set {axis}");

    public void SetAutomatic(CameraAxis axis, bool enabled)
    {
        var property = ToProperty(axis);
        var current = GetValue(axis) ?? 0;
        var flags = enabled ? CameraControlFlags.Auto : CameraControlFlags.Manual;
        ComHelper.ThrowOnFailure(control.Set(property, current, flags), $"set automatic {axis}");
    }

    private static CameraControlProperty ToProperty(CameraAxis axis) => axis switch
    {
        CameraAxis.Pan => CameraControlProperty.Pan,
        CameraAxis.Tilt => CameraControlProperty.Tilt,
        CameraAxis.Zoom => CameraControlProperty.Zoom,
        CameraAxis.Focus => CameraControlProperty.Focus,
        _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null),
    };
}
