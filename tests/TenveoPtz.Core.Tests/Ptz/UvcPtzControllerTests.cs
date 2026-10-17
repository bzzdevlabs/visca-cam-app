using System;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Tests.Fakes;

namespace TenveoPtz.Core.Tests.Ptz;

public sealed class UvcPtzControllerTests
{
    private readonly FakeCameraAxes axes = new();
    private readonly ManualTicker ticker = new();
    private readonly UvcPtzController controller;

    public UvcPtzControllerTests()
    {
        controller = new UvcPtzController(axes, ticker);
    }

    [Fact]
    public void Move_StepsImmediatelyAndOnEachTick()
    {
        controller.Move(Motion.Positive, Motion.None, 1.0);
        var afterFirstStep = axes.Values[CameraAxis.Pan];
        ticker.Fire();

        Assert.True(ticker.IsRunning);
        Assert.True(afterFirstStep > 0);
        Assert.True(axes.Values[CameraAxis.Pan] > afterFirstStep);
    }

    [Fact]
    public void Move_AtFullSpeedCrossesThePanRangeInSeveralSeconds()
    {
        axes.Values[CameraAxis.Pan] = -170;
        controller.Move(Motion.Positive, Motion.None, 1.0);

        // 40 ms ticks: 100 ticks is 4 seconds, still short of the 340 unit range.
        for (var i = 0; i < 100; i++)
        {
            ticker.Fire();
        }

        Assert.True(axes.Values[CameraAxis.Pan] < 170);
    }

    [Fact]
    public void Move_SlowSpeedAccumulatesFractionalSteps()
    {
        controller.Move(Motion.Positive, Motion.None, 0.0);
        for (var i = 0; i < 40; i++)
        {
            ticker.Fire();
        }

        var slow = axes.Values[CameraAxis.Pan];
        axes.Values[CameraAxis.Pan] = 0;
        controller.Move(Motion.None, Motion.None, 0.0);
        controller.Move(Motion.Positive, Motion.None, 0.5);
        for (var i = 0; i < 40; i++)
        {
            ticker.Fire();
        }

        // Slowest speed moves, but less than one unit per tick, and less than half speed.
        Assert.InRange(slow, 1, 39);
        Assert.True(axes.Values[CameraAxis.Pan] > slow);
    }

    [Fact]
    public void Move_SpeedChangeWhileMovingKeepsPosition()
    {
        controller.Move(Motion.Positive, Motion.None, 1.0);
        ticker.Fire();
        var before = axes.Values[CameraAxis.Pan];

        controller.Move(Motion.Positive, Motion.None, 0.0);

        Assert.Equal(before, axes.Values[CameraAxis.Pan]);
        Assert.True(ticker.IsRunning);
    }

    [Fact]
    public void Move_StopsTickerWhenAllAxesStop()
    {
        controller.Move(Motion.Positive, Motion.Positive, 0.5);

        controller.Move(Motion.None, Motion.None, 0.5);

        Assert.False(ticker.IsRunning);
    }

    [Fact]
    public void Move_ClampsToAxisRange()
    {
        axes.Values[CameraAxis.Tilt] = 89;

        controller.Move(Motion.None, Motion.Positive, 1.0);
        ticker.Fire();

        Assert.Equal(90, axes.Values[CameraAxis.Tilt]);
    }

    [Fact]
    public void Move_UsesContinuousMotionWhenTheCameraOffersIt()
    {
        axes.SpeedRanges[CameraAxis.Pan] = new AxisRange(1, 24, 1, 1);

        var controller = Continuous();
        controller.Move(Motion.Negative, Motion.None, 1.0);

        Assert.Equal(new[] { "Pan -12" }, axes.Speeds);
        Assert.False(axes.Values.ContainsKey(CameraAxis.Pan));
        Assert.False(ticker.IsRunning);
    }

    [Fact]
    public void Move_ContinuousSlowestSpeedIsTheCameraMinimum()
    {
        axes.SpeedRanges[CameraAxis.Pan] = new AxisRange(1, 24, 1, 1);

        var controller = Continuous();
        controller.Move(Motion.Positive, Motion.None, 0.0);

        Assert.Equal(new[] { "Pan 1" }, axes.Speeds);
    }

    [Fact]
    public void Move_ContinuousStopSendsZeroOnce()
    {
        axes.SpeedRanges[CameraAxis.Tilt] = new AxisRange(1, 20, 1, 1);
        var controller = Continuous();
        controller.Move(Motion.None, Motion.Positive, 0.5);

        controller.Move(Motion.None, Motion.None, 0.5);
        controller.Move(Motion.None, Motion.None, 0.5);

        Assert.Equal("Tilt 0", axes.Speeds[axes.Speeds.Count - 1]);
        Assert.Equal(2, axes.Speeds.Count);
    }

    [Fact]
    public void Home_StopsContinuousMotion()
    {
        axes.SpeedRanges[CameraAxis.Pan] = new AxisRange(1, 24, 1, 1);
        var controller = Continuous();
        controller.Move(Motion.Positive, Motion.None, 0.5);

        controller.Home();

        Assert.Equal("Pan 0", axes.Speeds[axes.Speeds.Count - 1]);
    }

    [Fact]
    public void Move_IgnoresContinuousMotionUnlessEnabled()
    {
        axes.SpeedRanges[CameraAxis.Pan] = new AxisRange(1, 24, 1, 1);

        controller.Move(Motion.Positive, Motion.None, 1.0);

        Assert.Empty(axes.Speeds);
        Assert.True(axes.Values[CameraAxis.Pan] > 0);
    }

    [Fact]
    public void StorePreset_UsesTheCommandedPositionNotALaggingReadback()
    {
        var preset = new Preset { Name = "A", Slot = 1 };
        controller.RecallPreset(new Preset { Name = "Target", Slot = 2, Position = new PtzPosition(100, 20, 300) });

        // The camera is still on its way and reports where it is now.
        axes.Values[CameraAxis.Pan] = 37;
        controller.StorePreset(preset);

        Assert.Equal(100, preset.Position?.Pan);
        Assert.Equal(20, preset.Position?.Tilt);
        Assert.Equal(300, preset.Position?.Zoom);
    }

    [Fact]
    public void StorePreset_ReadsTheCameraAfterAContinuousMove()
    {
        var controller = Continuous();
        axes.SpeedRanges[CameraAxis.Pan] = new AxisRange(1, 24, 1, 1);
        controller.RecallPreset(new Preset { Name = "Target", Slot = 2, Position = new PtzPosition(100, 0, 0) });
        controller.Move(Motion.Positive, Motion.None, 0.5);
        controller.Move(Motion.None, Motion.None, 0.5);
        axes.Values[CameraAxis.Pan] = 120;
        var preset = new Preset { Name = "A", Slot = 1 };

        controller.StorePreset(preset);

        Assert.Equal(120, preset.Position?.Pan);
    }

    [Fact]
    public void Zoom_ThrowsWhenAxisUnsupported()
    {
        axes.Ranges.Remove(CameraAxis.Zoom);

        Assert.Throws<NotSupportedException>(() => controller.Zoom(Motion.Positive, 1.0));
    }

    [Fact]
    public void Presets_RoundTripAbsolutePosition()
    {
        var preset = new Preset { Name = "Lectern", Slot = 1 };
        axes.Values[CameraAxis.Pan] = 42;
        axes.Values[CameraAxis.Tilt] = -10;
        axes.Values[CameraAxis.Zoom] = 300;
        controller.StorePreset(preset);
        axes.Values.Clear();

        controller.RecallPreset(preset);

        Assert.Equal(42, axes.Values[CameraAxis.Pan]);
        Assert.Equal(-10, axes.Values[CameraAxis.Tilt]);
        Assert.Equal(300, axes.Values[CameraAxis.Zoom]);
    }

    [Fact]
    public void RecallPreset_WithoutPositionExplainsWhy()
    {
        var preset = new Preset { Name = "Saved over VISCA", Slot = 3 };

        var error = Assert.Throws<InvalidOperationException>(() => controller.RecallPreset(preset));
        Assert.Contains("VISCA", error.Message);
    }

    [Fact]
    public void Home_GoesToDefaultPanAndTilt()
    {
        axes.Values[CameraAxis.Pan] = 100;
        axes.Values[CameraAxis.Tilt] = 50;

        controller.Home();

        Assert.Equal(0, axes.Values[CameraAxis.Pan]);
        Assert.Equal(0, axes.Values[CameraAxis.Tilt]);
    }

    [Fact]
    public void Dispose_ReleasesTicker()
    {
        controller.Dispose();

        Assert.True(ticker.Disposed);
    }

    private UvcPtzController Continuous() => new(axes, new ManualTicker(), useContinuousMoves: true);

    [Fact]
    public void Focus_WithoutAFocusAxisSaysSo()
    {
        var error = Assert.Throws<NotSupportedException>(() => controller.Focus(Motion.Positive));

        Assert.Equal("This camera does not expose focus control over USB.", error.Message);
    }

    [Fact]
    public void Focus_MovesTheFocusAxis()
    {
        axes.Ranges[CameraAxis.Focus] = new AxisRange(0, 100, 1, 50);
        axes.Values[CameraAxis.Focus] = 50;

        controller.Focus(Motion.Positive);
        for (var i = 0; i < 25; i++)
        {
            ticker.Fire(); // Focus moves at a fixed, slow speed: one second of ticks.
        }

        Assert.True(axes.Values[CameraAxis.Focus] > 50);
    }

    [Fact]
    public void AutoFocus_SwitchesTheCameraMode()
    {
        axes.Ranges[CameraAxis.Focus] = new AxisRange(0, 100, 1, 50);

        controller.SetAutoFocus(false);

        Assert.False(axes.Automatic[CameraAxis.Focus]);
    }

    [Fact]
    public void ForgetPreset_LeavesTheCameraAlone()
    {
        controller.ForgetPreset(new Preset { Name = "Wide", Slot = 1 });

        Assert.Empty(axes.Values);
    }

    [Fact]
    public void CameraErrorDuringAMove_StopsAndIsReported()
    {
        Exception? reported = null;
        controller.Faulted += (_, e) => reported = e.GetException();
        controller.Move(Motion.Positive, Motion.None, 1.0);
        axes.FailWith = new InvalidOperationException("unplugged");

        ticker.Fire();

        Assert.Equal("unplugged", reported?.Message);
        Assert.False(ticker.IsRunning);
    }
}
