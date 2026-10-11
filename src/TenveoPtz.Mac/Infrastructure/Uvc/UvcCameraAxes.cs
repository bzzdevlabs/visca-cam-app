using System;
using System.Collections.Generic;
using System.Diagnostics;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Mac.Infrastructure.Uvc;

/// <summary>
/// Adapts the UVC camera terminal controls (UVC 1.5, section 4.2.2.1) to <see cref="ICameraAxes"/>.
/// Pan and tilt share one control, so setting one axis also sends the other's target.
/// </summary>
internal sealed class UvcCameraAxes : ICameraAxes, IDisposable
{
    private const byte FocusAuto = 0x08;
    private const byte FocusAbsolute = 0x06;
    private const byte ZoomAbsolute = 0x0B;
    private const byte PanTiltAbsolute = 0x0D;

    /// <summary>How long the last pan/tilt target is trusted over the camera's current position (it may still be moving).</summary>
    private static readonly TimeSpan TargetLifetime = TimeSpan.FromSeconds(1);

    private readonly UvcDevice device;
    private readonly Dictionary<CameraAxis, AxisRange?> ranges = new();
    private readonly Stopwatch sinceTarget = new();
    private (int Pan, int Tilt) target;

    public UvcCameraAxes(UvcDevice device)
    {
        this.device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public AxisRange? GetRange(CameraAxis axis)
    {
        if (!ranges.TryGetValue(axis, out var range))
        {
            range = ReadRange(axis);
            ranges[axis] = range;
        }

        return range;
    }

    public int? GetValue(CameraAxis axis) => Read(axis, UvcDevice.GetCurrent);

    public void SetValue(CameraAxis axis, int value)
    {
        // Cameras may stall on values between two resolution steps.
        if (GetRange(axis) is { } range)
        {
            value = range.Clamp(range.Min + ((int)Math.Round((value - range.Min) / (double)range.Step) * range.Step));
        }

        switch (axis)
        {
            case CameraAxis.Pan:
            case CameraAxis.Tilt:
                var (pan, tilt) = CurrentTarget();
                target = axis == CameraAxis.Pan ? (value, tilt) : (pan, value);
                var data = new byte[8];
                BitConverter.TryWriteBytes(data.AsSpan(0, 4), target.Pan);
                BitConverter.TryWriteBytes(data.AsSpan(4, 4), target.Tilt);
                device.Set(PanTiltAbsolute, data);
                sinceTarget.Restart();
                break;
            default:
                device.Set(Selector(axis), BitConverter.GetBytes((ushort)value));
                break;
        }
    }

    public void SetAutomatic(CameraAxis axis, bool enabled)
    {
        if (axis != CameraAxis.Focus)
        {
            throw new NotSupportedException($"UVC cameras have no automatic {axis.ToString().ToLowerInvariant()} mode.");
        }

        device.Set(FocusAuto, new[] { enabled ? (byte)1 : (byte)0 });
    }

    public void Dispose() => device.Dispose();

    private (int Pan, int Tilt) CurrentTarget()
    {
        if (sinceTarget.IsRunning && sinceTarget.Elapsed < TargetLifetime)
        {
            return target;
        }

        var data = new byte[8];
        return device.TryGet(UvcDevice.GetCurrent, PanTiltAbsolute, data)
            ? (BitConverter.ToInt32(data, 0), BitConverter.ToInt32(data, 4))
            : (0, 0);
    }

    private AxisRange? ReadRange(CameraAxis axis)
    {
        var min = Read(axis, UvcDevice.GetMinimum);
        var max = Read(axis, UvcDevice.GetMaximum);
        if (min is null || max is null || max <= min)
        {
            return null;
        }

        var step = Read(axis, UvcDevice.GetResolution) ?? 1;
        var defaultValue = Read(axis, UvcDevice.GetDefault) ?? min.Value;
        return new AxisRange(min.Value, max.Value, step, defaultValue);
    }

    private int? Read(CameraAxis axis, byte request)
    {
        switch (axis)
        {
            case CameraAxis.Pan:
            case CameraAxis.Tilt:
                var panTilt = new byte[8];
                return device.TryGet(request, PanTiltAbsolute, panTilt)
                    ? BitConverter.ToInt32(panTilt, axis == CameraAxis.Pan ? 0 : 4)
                    : null;
            default:
                var data = new byte[2];
                return device.TryGet(request, Selector(axis), data) ? BitConverter.ToUInt16(data, 0) : null;
        }
    }

    private static byte Selector(CameraAxis axis) => axis switch
    {
        CameraAxis.Zoom => ZoomAbsolute,
        CameraAxis.Focus => FocusAbsolute,
        _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null),
    };
}
