using System;
using System.Collections.Generic;
using TenveoPtz.Core.Execution;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Simulation;

/// <summary>
/// A PTZ camera made of software, standing in for a UVC camera in tests and in the app's
/// <c>--simulator</c> mode. Like a real camera, its motors take time: an absolute target is
/// reached at a limited speed and the position read back lags while it moves. It can also offer
/// continuous (relative) motion. Directions follow the Tenveo cameras: the view turns right when
/// pan decreases and looks up when tilt decreases. <see cref="SimulatedCameraQuirks"/> reproduce
/// the ways real cameras differ.
/// </summary>
public sealed class SimulatedCamera : ICameraAxes, IDisposable
{
    /// <summary>Interval of the motor simulation.</summary>
    public static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(20);

    /// <summary>Fastest continuous speed setting, as reported by <see cref="GetSpeedRange"/>.</summary>
    public const int MaxContinuousSpeed = 24;

    private static readonly Dictionary<CameraAxis, AxisSpec> Specs = new()
    {
        // Range, default and motor speed (units per second) of each axis.
        [CameraAxis.Pan] = new AxisSpec(new AxisRange(-170, 170, 1, 0), 90),
        [CameraAxis.Tilt] = new AxisSpec(new AxisRange(-90, 30, 1, 0), 60),
        [CameraAxis.Zoom] = new AxisSpec(new AxisRange(0, 1000, 1, 0), 500),
        [CameraAxis.Focus] = new AxisSpec(new AxisRange(0, 255, 1, 128), 300),
    };

    /// <summary>How far behind the real position <see cref="SimulatedCameraQuirks.LaggingReadback"/> reports, in ticks.</summary>
    private const int ReadbackLagTicks = 15;

    private readonly ITicker ticker;
    private readonly bool continuousMoves;
    private readonly SimulatedCameraQuirks quirks;
    private readonly Dictionary<CameraAxis, AxisState> axes = new();
    private bool moving;
    private int idleTicks;

    /// <param name="ticker">Ticker driving the motors, firing every <see cref="TickInterval"/>. Ownership is transferred.</param>
    /// <param name="continuousMoves">Whether pan, tilt and zoom offer continuous (relative) motion.</param>
    public SimulatedCamera(ITicker ticker, bool continuousMoves = false, SimulatedCameraQuirks quirks = SimulatedCameraQuirks.None)
    {
        this.ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
        this.continuousMoves = continuousMoves;
        this.quirks = quirks;
        foreach (var entry in Specs)
        {
            axes[entry.Key] = new AxisState(entry.Value.Range.Default);
        }

        this.ticker.Tick += (_, _) => Step(TickInterval.TotalSeconds);
    }

    /// <summary>The camera moved; raised on every motor step.</summary>
    public event EventHandler? Moved;

    /// <summary>All motors stopped after moving.</summary>
    public event EventHandler? Settled;

    public bool IsMoving => moving;

    /// <summary>Exact position of an axis, between whole units while it moves.</summary>
    public double PositionOf(CameraAxis axis) => axes[axis].Position;

    public AxisRange? GetRange(CameraAxis axis) => Specs[axis].Range;

    public int? GetValue(CameraAxis axis)
    {
        var state = axes[axis];
        var reported = (quirks & SimulatedCameraQuirks.ReadbackIsLastTarget) != 0 ? state.LastAbsoluteTarget
            : (quirks & SimulatedCameraQuirks.LaggingReadback) != 0 && state.History.Count > 0 ? state.History.Peek()
            : state.Position;
        return (int)Math.Round(reported);
    }

    public void SetValue(CameraAxis axis, int value)
    {
        var state = axes[axis];
        state.Target = Specs[axis].Range.Clamp(value);
        state.LastAbsoluteTarget = state.Target.Value;
        state.Speed = 0;
        Wake();
    }

    public void SetAutomatic(CameraAxis axis, bool enabled) => axes[axis].Automatic = enabled;

    public AxisRange? GetSpeedRange(CameraAxis axis) =>
        continuousMoves && axis != CameraAxis.Focus ? new AxisRange(1, MaxContinuousSpeed, 1, 1) : null;

    public void SetSpeed(CameraAxis axis, int speed)
    {
        if (GetSpeedRange(axis) is null)
        {
            throw new NotSupportedException($"The simulated camera has no continuous {axis.ToString().ToLowerInvariant()} control.");
        }

        if ((quirks & SimulatedCameraQuirks.IgnoresContinuousMoves) != 0)
        {
            return;
        }

        var state = axes[axis];
        state.Target = null;
        state.Speed = Math.Max(-MaxContinuousSpeed, Math.Min(MaxContinuousSpeed, speed));
        Wake();
    }

    public void Dispose() => ticker.Dispose();

    /// <summary>Advances the motors by <paramref name="seconds"/>.</summary>
    private void Step(double seconds)
    {
        var anyMoving = false;
        foreach (var entry in axes)
        {
            var spec = Specs[entry.Key];
            var state = entry.Value;
            var before = state.Position;
            if (state.Speed != 0)
            {
                // Continuous motion: the fastest setting runs the motor at full speed.
                state.Position += spec.UnitsPerSecond * seconds * state.Speed / MaxContinuousSpeed;
            }
            else if (state.Target is { } target)
            {
                var step = spec.UnitsPerSecond * seconds;
                var remaining = target - state.Position;
                state.Position = Math.Abs(remaining) <= step ? target : state.Position + (Math.Sign(remaining) * step);
                if (state.Position == target)
                {
                    state.Target = null;
                }
            }

            state.Position = Math.Max(spec.Range.Min, Math.Min(spec.Range.Max, state.Position));
            if (state.Position != before || state.Speed != 0 || state.Target is not null)
            {
                anyMoving = true;
            }

            state.History.Enqueue(state.Position);
            while (state.History.Count > ReadbackLagTicks)
            {
                state.History.Dequeue();
            }
        }

        idleTicks = anyMoving ? 0 : idleTicks + 1;
        if (anyMoving)
        {
            Moved?.Invoke(this, EventArgs.Empty);
        }
        else if (idleTicks >= ReadbackLagTicks)
        {
            // Stopped long enough for a lagging readback to have caught up.
            ticker.Stop();
            if (moving)
            {
                moving = false;
                Settled?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void Wake()
    {
        moving = true;
        idleTicks = 0;
        if (!ticker.IsRunning)
        {
            ticker.Start();
        }
    }

    private sealed class AxisSpec
    {
        public AxisSpec(AxisRange range, double unitsPerSecond)
        {
            Range = range;
            UnitsPerSecond = unitsPerSecond;
        }

        public AxisRange Range { get; }

        public double UnitsPerSecond { get; }
    }

    private sealed class AxisState
    {
        public AxisState(int position)
        {
            Position = position;
            LastAbsoluteTarget = position;
        }

        public double Position { get; set; }

        public double LastAbsoluteTarget { get; set; }

        /// <summary>Recent positions, oldest first, for <see cref="SimulatedCameraQuirks.LaggingReadback"/>.</summary>
        public Queue<double> History { get; } = new();

        public int? Target { get; set; }

        public int Speed { get; set; }

        public bool Automatic { get; set; }
    }
}

/// <summary>Ways real UVC cameras differ, for <see cref="SimulatedCamera"/> to reproduce.</summary>
[Flags]
public enum SimulatedCameraQuirks
{
    None = 0,

    /// <summary>The position read back is a few hundred milliseconds old (firmware latency).</summary>
    LaggingReadback = 1,

    /// <summary>The position read back is the last absolute target: continuous moves do not update it.</summary>
    ReadbackIsLastTarget = 2,

    /// <summary>Continuous (relative) controls are reported but do nothing.</summary>
    IgnoresContinuousMoves = 4,
}
