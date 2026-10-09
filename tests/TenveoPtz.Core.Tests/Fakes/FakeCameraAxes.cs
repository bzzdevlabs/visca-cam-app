using System.Collections.Generic;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Tests.Fakes;

internal sealed class FakeCameraAxes : ICameraAxes
{
    public Dictionary<CameraAxis, AxisRange> Ranges { get; } = new()
    {
        [CameraAxis.Pan] = new AxisRange(-170, 170, 1, 0),
        [CameraAxis.Tilt] = new AxisRange(-30, 90, 1, 0),
        [CameraAxis.Zoom] = new AxisRange(0, 1000, 1, 0),
    };

    public Dictionary<CameraAxis, int> Values { get; } = new();

    public Dictionary<CameraAxis, bool> Automatic { get; } = new();

    public AxisRange? GetRange(CameraAxis axis) => Ranges.TryGetValue(axis, out var range) ? range : null;

    public int? GetValue(CameraAxis axis) => Values.TryGetValue(axis, out var value) ? value : null;

    public void SetValue(CameraAxis axis, int value) => Values[axis] = value;

    public void SetAutomatic(CameraAxis axis, bool enabled) => Automatic[axis] = enabled;
}
