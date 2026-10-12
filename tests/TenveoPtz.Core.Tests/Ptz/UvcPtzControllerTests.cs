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
}
