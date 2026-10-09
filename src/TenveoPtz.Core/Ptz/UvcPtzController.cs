using System;
using System.Collections.Generic;
using System.IO;
using TenveoPtz.Core.Execution;
using TenveoPtz.Core.Presets;

namespace TenveoPtz.Core.Ptz;

/// <summary>
/// PTZ strategy using the UVC camera controls exposed by the USB video driver. UVC only offers
/// absolute positioning, so continuous moves are emulated by stepping the axis on every tick.
/// Must be used from the thread that owns the camera (the UI thread).
/// </summary>
public sealed class UvcPtzController : IPtzController
{
    /// <summary>Fraction of an axis range travelled per tick at full speed.</summary>
    internal const double FullSpeedStepFraction = 0.02;

    /// <summary>Fraction of full speed used at the slowest speed setting.</summary>
    internal const double MinimumSpeedFactor = 0.1;

    private readonly ICameraAxes axes;
    private readonly ITicker ticker;
    private readonly Dictionary<CameraAxis, AxisMotion> motions = new();

    /// <param name="ticker">Ticker driving continuous moves. Ownership is transferred to the controller.</param>
    public UvcPtzController(ICameraAxes axes, ITicker ticker)
    {
        this.axes = axes ?? throw new ArgumentNullException(nameof(axes));
        this.ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
        this.ticker.Tick += OnTick;
    }

    public event EventHandler<ErrorEventArgs>? Faulted;

    public string Description => "UVC (USB)";

    public void Move(Motion pan, Motion tilt, double speed)
    {
        SetMotion(CameraAxis.Pan, pan, speed);
        SetMotion(CameraAxis.Tilt, tilt, speed);
    }

    public void Zoom(Motion direction, double speed) => SetMotion(CameraAxis.Zoom, direction, speed);

    public void Focus(Motion direction) => SetMotion(CameraAxis.Focus, direction, 0.3);

    public void SetAutoFocus(bool enabled)
    {
        RequireRange(CameraAxis.Focus);
        axes.SetAutomatic(CameraAxis.Focus, enabled);
    }

    public void Home()
    {
        StopAll();
        MoveTo(CameraAxis.Pan, RequireRange(CameraAxis.Pan).Default);
        MoveTo(CameraAxis.Tilt, RequireRange(CameraAxis.Tilt).Default);
    }

    public void StorePreset(Preset preset)
    {
        RequirePreset(preset).Position = new PtzPosition(
            axes.GetValue(CameraAxis.Pan),
            axes.GetValue(CameraAxis.Tilt),
            axes.GetValue(CameraAxis.Zoom));
    }

    public void RecallPreset(Preset preset)
    {
        var position = RequirePreset(preset).Position
            ?? throw new InvalidOperationException(
                $"Preset \"{preset.Name}\" was saved in VISCA mode; save it again in UVC mode to use it here.");

        StopAll();
        MoveTo(CameraAxis.Pan, position.Pan);
        MoveTo(CameraAxis.Tilt, position.Tilt);
        MoveTo(CameraAxis.Zoom, position.Zoom);
    }

    public void ForgetPreset(Preset preset)
    {
        // Positions are stored on the computer only; nothing to release on the camera.
    }

    public void Dispose()
    {
        ticker.Tick -= OnTick;
        ticker.Dispose();
        motions.Clear();
    }

    private static Preset RequirePreset(Preset preset) => preset ?? throw new ArgumentNullException(nameof(preset));

    private static int StepFor(AxisRange range, double speed)
    {
        var factor = MinimumSpeedFactor + ((1 - MinimumSpeedFactor) * SpeedScale.Clamp(speed));
        var step = (int)Math.Round(range.Span * FullSpeedStepFraction * factor);
        return Math.Max(range.Step, step);
    }

    private AxisRange RequireRange(CameraAxis axis) =>
        axes.GetRange(axis) ?? throw new NotSupportedException($"This camera does not expose {axis.ToString().ToLowerInvariant()} control over USB.");

    private void SetMotion(CameraAxis axis, Motion direction, double speed)
    {
        if (direction == Motion.None)
        {
            motions.Remove(axis);
        }
        else
        {
            var range = RequireRange(axis);
            var current = axes.GetValue(axis) ?? range.Default;
            motions[axis] = new AxisMotion(range, direction, StepFor(range, speed), current);
            Advance(axis, motions[axis]);
        }

        UpdateTicker();
    }

    private void MoveTo(CameraAxis axis, int? value)
    {
        var range = axes.GetRange(axis);
        if (range is not null && value.HasValue)
        {
            axes.SetValue(axis, range.Clamp(value.Value));
        }
    }

    private void StopAll()
    {
        motions.Clear();
        UpdateTicker();
    }

    private void UpdateTicker()
    {
        if (motions.Count == 0)
        {
            ticker.Stop();
        }
        else if (!ticker.IsRunning)
        {
            ticker.Start();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        try
        {
            foreach (var entry in new List<KeyValuePair<CameraAxis, AxisMotion>>(motions))
            {
                Advance(entry.Key, entry.Value);
            }
        }
        catch (Exception ex)
        {
            StopAll();
            Faulted?.Invoke(this, new ErrorEventArgs(ex));
        }
    }

    private void Advance(CameraAxis axis, AxisMotion motion)
    {
        var next = motion.Range.Clamp(motion.Position + ((int)motion.Direction * motion.Step));
        if (next != motion.Position)
        {
            axes.SetValue(axis, next);
            motion.Position = next;
        }
    }

    private sealed class AxisMotion
    {
        public AxisMotion(AxisRange range, Motion direction, int step, int position)
        {
            Range = range;
            Direction = direction;
            Step = step;
            Position = position;
        }

        public AxisRange Range { get; }

        public Motion Direction { get; }

        public int Step { get; }

        public int Position { get; set; }
    }
}
