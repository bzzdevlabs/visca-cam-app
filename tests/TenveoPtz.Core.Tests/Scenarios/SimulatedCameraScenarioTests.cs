using System;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Session;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Simulation;
using TenveoPtz.Core.Tests.Fakes;

namespace TenveoPtz.Core.Tests.Scenarios;

/// <summary>
/// End-to-end checks of what a user does, from the view's events through the presenter, the real
/// session factory and UVC controller, to a simulated camera whose motors take time to move.
/// Every scenario runs against cameras that behave differently (see <see cref="SimulatedCameraQuirks"/>),
/// with absolute-step moves (the default) and, where the camera honours them, continuous moves.
/// </summary>
public sealed class SimulatedCameraScenarioTests : IDisposable
{
    private readonly SimulatedClock clock = new();
    private readonly FakeView view = new();
    private readonly MemoryStore<AppSettings> settingsStore = new();
    private readonly MemoryStore<PresetBook> presetStore = new();
    private SimulatorDevices devices = null!;
    private MainPresenter presenter = null!;

    private SimulatedCamera Camera => devices.Camera!;

    // Positions at the camera's resolution (whole units): a continuous move can stop between two.
    private double Pan => Math.Round(Camera.PositionOf(CameraAxis.Pan));

    private double Tilt => Math.Round(Camera.PositionOf(CameraAxis.Tilt));

    private double ZoomLevel => Math.Round(Camera.PositionOf(CameraAxis.Zoom));

    public void Dispose() => presenter?.Dispose();

    [Theory]
    [MemberData(nameof(Cameras))]
    public void PadDirections_MoveTheCameraTheExpectedWay(bool continuous, SimulatedCameraQuirks quirks)
    {
        Connect(continuous, quirks);

        // Tenveo cameras turn right as pan decreases and look up as tilt decreases.
        Hold(Motion.Positive, Motion.None, 1);
        Assert.True(Pan < -3, $"Right should decrease pan, got {Pan}.");

        var afterRight = Pan;
        Hold(Motion.Negative, Motion.None, 1);
        Assert.True(Pan > afterRight + 3, $"Left should increase pan, got {Pan}.");

        Hold(Motion.None, Motion.Positive, 1);
        Assert.True(Tilt < -3, $"Up should decrease tilt, got {Tilt}.");
        Assert.True(-Tilt > Math.Abs(afterRight - Pan) / 4, "Tilt should not crawl next to pan.");

        var afterUp = Tilt;
        Hold(Motion.None, Motion.Negative, 1);
        Assert.True(Tilt > afterUp + 3, $"Down should increase tilt, got {Tilt}.");
    }

    [Theory]
    [MemberData(nameof(Cameras))]
    public void Zoom_InAndOut(bool continuous, SimulatedCameraQuirks quirks)
    {
        Connect(continuous, quirks);

        HoldZoom(Motion.Positive, 1);
        var zoomedIn = ZoomLevel;
        Assert.True(zoomedIn > 10, $"Zoom in should increase zoom, got {zoomedIn}.");

        HoldZoom(Motion.Negative, 0.5);
        Assert.True(ZoomLevel < zoomedIn, $"Zoom out should decrease zoom, got {ZoomLevel}.");
    }

    [Theory]
    [MemberData(nameof(Cameras))]
    public void Release_StopsTheCamera(bool continuous, SimulatedCameraQuirks quirks)
    {
        Connect(continuous, quirks);
        Hold(Motion.Positive, Motion.Positive, 1);
        var stopped = (Pan, Tilt);

        clock.Advance(3);

        Assert.Equal(stopped, (Pan, Tilt));
        Assert.False(Camera.IsMoving);
    }

    [Theory]
    [MemberData(nameof(Cameras))]
    public void Presets_SavedRightAfterAMove_ComeBackToWhereTheCameraStopped(bool continuous, SimulatedCameraQuirks quirks)
    {
        Connect(continuous, quirks);

        // Saved straight after releasing: the camera may still be travelling to its last target.
        Press(Motion.Positive, Motion.None, 1.5);
        Release();
        SettleContinuousReadback(continuous);
        SavePreset("Left side");
        clock.Advance(3);
        var first = (Pan, Tilt, ZoomLevel);

        Press(Motion.None, Motion.Positive, 1);
        HoldZoom(Motion.Positive, 0.5);
        Release();
        SettleContinuousReadback(continuous);
        SavePreset("Up close");
        clock.Advance(3);
        var second = (Pan, Tilt, ZoomLevel);
        Assert.NotEqual(first, second);

        foreach (var (index, expected) in new[] { (0, first), (1, second), (0, first), (1, second) })
        {
            view.RecallPresetAt(index);
            clock.Advance(5);
            Assert.False(Camera.IsMoving);
            Assert.Equal(expected, (Pan, Tilt, ZoomLevel));
        }
    }

    [Theory]
    [MemberData(nameof(Cameras))]
    public void Presets_SurviveAReconnection(bool continuous, SimulatedCameraQuirks quirks)
    {
        Connect(continuous, quirks);
        Hold(Motion.Negative, Motion.Negative, 1);
        SavePreset("A");
        clock.Advance(3);
        var saved = (Pan, Tilt);

        view.Disconnect();
        view.Connect();
        view.RecallPresetAt(0);
        clock.Advance(5);

        Assert.Equal(saved, (Pan, Tilt));
    }

    /// <summary>Step moves only: a continuous move starts wherever the camera physically is.</summary>
    [Fact]
    public void MovingFromAPreset_StartsFromThePreset()
    {
        Connect(continuous: false, SimulatedCameraQuirks.LaggingReadback);
        Hold(Motion.Positive, Motion.None, 1.5);
        SavePreset("A");
        clock.Advance(3);
        var preset = Pan;
        view.Home();
        clock.Advance(5);

        // Recall, then move before the camera has arrived: the move continues from the preset.
        view.RecallPresetAt(0);
        clock.Advance(0.1);
        Hold(Motion.Positive, Motion.None, 0.5);
        clock.Advance(5);

        Assert.True(Pan < preset, $"Expected to end right of the preset ({preset}), got {Pan}.");
    }

    [Theory]
    [MemberData(nameof(Cameras))]
    public void Home_GoesBackToTheCentre(bool continuous, SimulatedCameraQuirks quirks)
    {
        Connect(continuous, quirks);
        Hold(Motion.Positive, Motion.Positive, 1);

        view.Home();
        clock.Advance(5);

        Assert.Equal((0.0, 0.0), (Pan, Tilt));
    }

    [Theory]
    [MemberData(nameof(Cameras))]
    public void SpeedSettings_ChangeHowFarAMoveGoes(bool continuous, SimulatedCameraQuirks quirks)
    {
        Connect(continuous, quirks);
        view.ChangeSpeed(1.0);
        var fast = Distance(() => Hold(Motion.Positive, Motion.None, 1));

        view.ChangeSpeed(0.0);
        var slow = Distance(() => Hold(Motion.Positive, Motion.None, 1));

        view.ChangeSpeed(1.0);
        view.SetPreciseMode(true);
        var precise = Distance(() => Hold(Motion.Positive, Motion.None, 1));

        Assert.True(slow > 0, "The slowest speed should still move.");
        Assert.True(fast > 2 * slow, $"Fast ({fast}) should be well above slow ({slow}).");
        Assert.True(precise < fast / 2, $"Precise ({precise}) should be well below fast ({fast}).");
    }

    /// <summary>Moves that work on every camera, and the continuous moves the cameras that honour them.</summary>
    public static TheoryData<bool, SimulatedCameraQuirks> Cameras() => new()
    {
        { false, SimulatedCameraQuirks.None },
        { false, SimulatedCameraQuirks.LaggingReadback },
        { false, SimulatedCameraQuirks.ReadbackIsLastTarget },
        { false, SimulatedCameraQuirks.IgnoresContinuousMoves },
        { true, SimulatedCameraQuirks.None },
        { true, SimulatedCameraQuirks.LaggingReadback },
    };

    [Theory]
    [InlineData(SimulatedCameraQuirks.IgnoresContinuousMoves)]
    [InlineData(SimulatedCameraQuirks.ReadbackIsLastTarget)]
    public void DefaultSettings_WorkOnCamerasWhoseContinuousControlsMisbehave(SimulatedCameraQuirks quirks)
    {
        Connect(continuous: false, quirks, cameraOffersContinuous: true, defaultSettings: true);

        Hold(Motion.Positive, Motion.None, 1);
        SavePreset("A");
        var saved = Pan;
        view.Home();
        clock.Advance(5);
        view.RecallPresetAt(0);
        clock.Advance(5);

        Assert.True(saved < -3, $"The camera should move, got {saved}.");
        Assert.Equal(saved, Pan);
    }

    /// <summary>Why continuous moves are off by default: the 0.5.2 controls went dead on such a camera.</summary>
    [Fact]
    public void ContinuousMoves_OnACameraThatIgnoresThem_DoNothing()
    {
        Connect(continuous: true, SimulatedCameraQuirks.IgnoresContinuousMoves, cameraOffersContinuous: true);

        Hold(Motion.Positive, Motion.None, 1);

        Assert.Equal(0, Pan);
    }

    /// <summary>Why continuous moves are off by default: presets saved after them are wrong on such a camera.</summary>
    [Fact]
    public void ContinuousMoves_OnACameraReportingItsLastTarget_SaveWrongPresets()
    {
        Connect(continuous: true, SimulatedCameraQuirks.ReadbackIsLastTarget, cameraOffersContinuous: true);

        Hold(Motion.Positive, Motion.None, 1);
        SavePreset("A");
        var reached = Pan;
        view.Home();
        clock.Advance(5);
        view.RecallPresetAt(0);
        clock.Advance(5);

        Assert.True(reached < -3);
        Assert.NotEqual(reached, Pan);
    }

    private void Connect(bool continuous, SimulatedCameraQuirks quirks, bool? cameraOffersContinuous = null, bool defaultSettings = false)
    {
        devices = new SimulatorDevices(clock, cameraOffersContinuous ?? continuous, quirks);
        var settings = new AppSettings
        {
            Connection = new ConnectionOptions { VideoDeviceName = SimulatorDevices.CameraName, Mode = ControlMode.Uvc, ShowPreview = false },
        };
        if (!defaultSettings)
        {
            settings.UvcContinuousMoves = continuous;
        }

        settingsStore.Save(settings);
        var log = new Logger(new LogHub(LogLevel.Info, 100), "Test");
        presenter = new MainPresenter(view, devices, devices, new CameraSessionFactory(devices, devices, clock, log), settingsStore, presetStore, log: log);
        view.Load();
        view.Connect();
        Assert.True(view.Connected, view.Status);
    }

    private void Press(Motion pan, Motion tilt, double seconds)
    {
        view.PanTilt(pan, tilt);
        clock.Advance(seconds);
    }

    private void Release() => view.PanTilt(Motion.None, Motion.None);

    /// <summary>Holds a direction, releases it and lets the camera settle.</summary>
    private void Hold(Motion pan, Motion tilt, double seconds)
    {
        Press(pan, tilt, seconds);
        Release();
        clock.Advance(3);
    }

    private void HoldZoom(Motion direction, double seconds)
    {
        view.Zoom(direction);
        clock.Advance(seconds);
        view.Zoom(Motion.None);
        clock.Advance(3);
    }

    /// <summary>
    /// Known limitation of continuous moves: where the camera stopped is only known by reading it
    /// back, which a lagging camera reports late. Step moves (the default) save the commanded
    /// position and need no wait.
    /// </summary>
    private void SettleContinuousReadback(bool continuous)
    {
        if (continuous)
        {
            clock.Advance(1);
        }
    }

    private void SavePreset(string name)
    {
        view.PromptAnswer = name;
        view.AddPreset();
        Assert.False(view.StatusIsError, view.Status);
    }

    private double Distance(Action move)
    {
        var before = Pan;
        move();
        return Math.Abs(Pan - before);
    }
}
