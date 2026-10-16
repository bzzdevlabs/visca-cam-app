using System;
using System.Collections.Generic;
using System.IO;
using TenveoPtz.Core.Execution;
using TenveoPtz.Core.Presets;

namespace TenveoPtz.Core.Ptz;

/// <summary>
/// PTZ strategy using the UVC camera controls exposed by the USB video driver. Moves are made by
/// stepping the absolute position on every tick. Optionally, pan, tilt and zoom use the camera's
/// own continuous (relative) motion instead; it is off by default because some cameras report it
/// without honouring it, which leaves the controls dead.
/// Presets store the position the controller last commanded rather than the one the camera
/// reports: a camera still travelling to its target reports a position it is about to leave.
/// Must be used from the thread that owns the camera (the UI thread).
/// </summary>
public sealed class UvcPtzController : IPtzController
{
    /// <summary>
    /// Fraction of an axis range travelled per tick at full speed: about 6 % of the range per
    /// second with the 40 ms ticks of <see cref="Session.CameraSessionFactory.UvcTickInterval"/>.
    /// </summary>
    internal const double FullSpeedStepFraction = 0.0025;

    /// <summary>Fraction of full speed used at the slowest speed setting.</summary>
    internal const double MinimumSpeedFactor = 0.05;

    /// <summary>Share of the camera's fastest continuous speed reached at the top of the slider.</summary>
    internal const double ContinuousSpeedShare = 0.5;

    private readonly ICameraAxes axes;
    private readonly ITicker ticker;
    private readonly Dictionary<CameraAxis, AxisMotion> motions = new();
    private readonly Dictionary<CameraAxis, AxisRange?> speedRanges = new();
    private readonly HashSet<CameraAxis> continuous = new();

    /// <summary>Last absolute position sent to each axis; absent when unknown (e.g. after a continuous move).</summary>
    private readonly Dictionary<CameraAxis, int> targets = new();
    private readonly bool useContinuousMoves;

    /// <param name="ticker">Ticker driving continuous moves. Ownership is transferred to the controller.</param>
    /// <param name="useContinuousMoves">Use the camera's continuous (relative) motion when it offers it.</param>
    public UvcPtzController(ICameraAxes axes, ITicker ticker, bool useContinuousMoves = false)
    {
        this.axes = axes ?? throw new ArgumentNullException(nameof(axes));
        this.ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
        this.useContinuousMoves = useContinuousMoves;
        this.ticker.Tick += OnTick;
    }

    /// <summary>Whether <paramref name="axis"/> moves with the camera's continuous motion (otherwise by absolute steps).</summary>
    public bool MovesContinuously(CameraAxis axis) => SpeedRangeOf(axis) is not null;

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
            PositionOf(CameraAxis.Pan),
            PositionOf(CameraAxis.Tilt),
            PositionOf(CameraAxis.Zoom));
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

    /// <summary>
    /// Distance travelled per tick. It may be a fraction of the axis resolution: the position is
    /// accumulated between ticks, so slow speeds move one unit every few ticks instead of rounding up.
    /// </summary>
    private double StepFor(CameraAxis axis, AxisRange range, double speed)
    {
        var factor = MinimumSpeedFactor + ((1 - MinimumSpeedFactor) * SpeedScale.Ease(speed));
        return SpanFor(axis, range) * FullSpeedStepFraction * factor;
    }

    /// <summary>
    /// Span the speed is a fraction of. Tilt uses the pan span: both are angles in the same unit,
    /// and tilt's much smaller range would otherwise make it crawl next to pan.
    /// </summary>
    private int SpanFor(CameraAxis axis, AxisRange range) =>
        axis == CameraAxis.Tilt && axes.GetRange(CameraAxis.Pan) is { } pan ? Math.Max(range.Span, pan.Span) : range.Span;

    private AxisRange RequireRange(CameraAxis axis) =>
        axes.GetRange(axis) ?? throw new NotSupportedException($"This camera does not expose {axis.ToString().ToLowerInvariant()} control over USB.");

    private void SetMotion(CameraAxis axis, Motion direction, double speed)
    {
        if (SpeedRangeOf(axis) is { } speedRange)
        {
            SetContinuous(axis, direction, speed, speedRange);
            return;
        }

        if (direction == Motion.None)
        {
            motions.Remove(axis);
        }
        else if (motions.TryGetValue(axis, out var motion) && motion.Direction == direction)
        {
            // Speed change while moving: keep going from where we are, just at the new pace.
            motion.Step = StepFor(axis, motion.Range, speed);
        }
        else
        {
            var range = RequireRange(axis);
            var current = PositionOf(axis) ?? range.Default;
            motions[axis] = new AxisMotion(range, direction, StepFor(axis, range, speed), current);
            Advance(axis, motions[axis]);
        }

        UpdateTicker();
    }

    private void SetContinuous(CameraAxis axis, Motion direction, double speed, AxisRange speedRange)
    {
        if (direction == Motion.None)
        {
            if (continuous.Remove(axis))
            {
                axes.SetSpeed(axis, 0);
            }

            return;
        }

        var fastest = Math.Max(speedRange.Min, (int)Math.Ceiling(speedRange.Max * ContinuousSpeedShare));
        axes.SetSpeed(axis, (int)direction * SpeedScale.ToRange(speed, speedRange.Min, fastest));
        continuous.Add(axis);

        // Where the camera stops is only known by reading it back.
        targets.Remove(axis);
    }

    /// <summary>Continuous speed range of pan, tilt and zoom, read once; <c>null</c> when only absolute moves work.</summary>
    private AxisRange? SpeedRangeOf(CameraAxis axis)
    {
        if (!useContinuousMoves || axis == CameraAxis.Focus)
        {
            return null;
        }

        if (!speedRanges.TryGetValue(axis, out var range))
        {
            range = axes.GetSpeedRange(axis);
            speedRanges[axis] = range;
        }

        return range;
    }

    private void MoveTo(CameraAxis axis, int? value)
    {
        var range = axes.GetRange(axis);
        if (range is not null && value.HasValue)
        {
            SetPosition(axis, range.Clamp(value.Value));
        }
    }

    private void SetPosition(CameraAxis axis, int value)
    {
        axes.SetValue(axis, value);
        targets[axis] = value;
    }

    /// <summary>Where the axis is going: the last position commanded, else the one the camera reports.</summary>
    private int? PositionOf(CameraAxis axis) => targets.TryGetValue(axis, out var target) ? target : axes.GetValue(axis);

    private void StopAll()
    {
        motions.Clear();
        UpdateTicker();
        foreach (var axis in new List<CameraAxis>(continuous))
        {
            continuous.Remove(axis);
            axes.SetSpeed(axis, 0);
        }
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
        var range = motion.Range;
        motion.Position = Math.Max(range.Min, Math.Min(range.Max, motion.Position + ((int)motion.Direction * motion.Step)));

        // Only whole multiples of the axis resolution reach the camera.
        var value = range.Clamp(range.Min + ((int)Math.Round((motion.Position - range.Min) / range.Step) * range.Step));
        if (value != motion.Written)
        {
            SetPosition(axis, value);
            motion.Written = value;
        }
    }

    private sealed class AxisMotion
    {
        public AxisMotion(AxisRange range, Motion direction, double step, int position)
        {
            Range = range;
            Direction = direction;
            Step = step;
            Position = position;
            Written = position;
        }

        public AxisRange Range { get; }

        public Motion Direction { get; }

        /// <summary>Distance per tick, in axis units (may be below one).</summary>
        public double Step { get; set; }

        /// <summary>Exact position, including the fraction not yet sent to the camera.</summary>
        public double Position { get; set; }

        /// <summary>Last value sent to the camera.</summary>
        public int Written { get; set; }
    }
}
