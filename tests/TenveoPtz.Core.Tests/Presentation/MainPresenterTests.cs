using System;
using System.Linq;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Tests.Fakes;

namespace TenveoPtz.Core.Tests.Presentation;

public sealed class MainPresenterTests : IDisposable
{
    private readonly FakeView view = new();
    private readonly FakeSessionFactory sessions = new();
    private readonly MemoryStore<AppSettings> settingsStore = new();
    private readonly MemoryStore<PresetBook> presetStore = new();
    private readonly LogHub log = new(LogLevel.Debug, 100);
    private readonly MainPresenter presenter;

    public MainPresenterTests()
    {
        var devices = new FakeDevices();
        presenter = new MainPresenter(view, devices, devices, sessions, settingsStore, presetStore, log: new Logger(log, "Main"));
    }

    private RecordingPtzController Ptz => sessions.Ptz;

    public void Dispose() => presenter.Dispose();

    [Fact]
    public void Load_AutoConnectsWhenPreviouslyConnected()
    {
        settingsStore.Save(new AppSettings { AutoConnect = true });

        view.Load();

        Assert.True(view.Connected);
        Assert.True(presenter.IsConnected);
    }

    [Fact]
    public void Connect_FailureIsShownAsError()
    {
        sessions.FailWith = new InvalidOperationException("COM3 is busy");
        view.Load();

        view.Connect();

        Assert.False(view.Connected);
        Assert.True(view.StatusIsError);
        Assert.Contains("COM3 is busy", view.Status);
    }

    [Fact]
    public void Connect_FailureIsLoggedWithTheException()
    {
        var failure = new InvalidOperationException("COM3 is busy");
        sessions.FailWith = failure;
        view.Load();

        view.Connect();

        var entry = log.Recent().Last();
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(failure, entry.Exception);
    }

    [Fact]
    public void CameraIntents_AreLoggedAtDebugLevel()
    {
        view.Load();
        view.Connect();

        view.Home();

        Assert.Contains(log.Recent(), e => e.Level == LogLevel.Debug && e.Message.StartsWith("Home"));
    }

    [Fact]
    public void RawCommand_RejectsInvalidHex()
    {
        view.Load();
        view.Connect();

        view.SendRaw("not hex");

        Assert.True(view.StatusIsError);
        Assert.Contains("hexadecimal", view.Status);
    }

    [Fact]
    public void RawCommand_NeedsAViscaConnection()
    {
        view.Load();
        view.Connect();

        view.SendRaw("81 01 06 04 FF");

        Assert.True(view.StatusIsError);
        Assert.Contains("VISCA", view.Status);
    }

    [Fact]
    public void RawCommand_WithoutConnectionReportsError()
    {
        view.Load();

        view.SendRaw("81 01 06 04 FF");

        Assert.True(view.StatusIsError);
        Assert.Contains("Not connected", view.Status);
    }

    [Fact]
    public void Disconnect_StopsCameraAndForgetsAutoConnect()
    {
        view.Load();
        view.Connect();

        view.Disconnect();

        Assert.Equal(new[] { "Move None None", "Zoom None", "Dispose" }, Ptz.Calls);
        Assert.False(settingsStore.Saved?.AutoConnect);
    }

    [Fact]
    public void PanTilt_WithoutConnectionReportsError()
    {
        view.Load();

        view.PanTilt(Motion.Positive, Motion.None);

        Assert.True(view.StatusIsError);
        Assert.Empty(Ptz.Calls);
    }

    [Fact]
    public void PanTilt_ReversesCameraDirectionsByDefault()
    {
        view.Load();
        view.Connect();

        view.PanTilt(Motion.Positive, Motion.Negative);

        Assert.Equal("Move Negative Positive", Assert.Single(Ptz.Calls));
    }

    [Fact]
    public void PanTilt_HonoursInvertSettings()
    {
        settingsStore.Save(new AppSettings { InvertPan = true });
        view.Load();
        view.Connect();

        view.PanTilt(Motion.Positive, Motion.Positive);

        Assert.Equal("Move Positive Negative", Assert.Single(Ptz.Calls));
    }

    [Fact]
    public void SpeedChange_WhileMovingIsAppliedImmediately()
    {
        view.Load();
        view.Connect();
        view.ChangeSpeed(0.2);
        view.PanTilt(Motion.Positive, Motion.None);
        view.Zoom(Motion.Positive);

        view.ChangeSpeed(0.7);

        Assert.Equal(new[] { "Move Negative None", "Zoom Positive", "Move Negative None", "Zoom Positive" }, Ptz.Calls);
        Assert.Equal(0.7, Ptz.LastSpeed);
    }

    [Fact]
    public void SpeedChange_WhenStoppedSendsNothing()
    {
        view.Load();
        view.Connect();
        view.PanTilt(Motion.Positive, Motion.None);
        view.PanTilt(Motion.None, Motion.None);
        Ptz.Calls.Clear();

        view.ChangeSpeed(0.7);

        Assert.Empty(Ptz.Calls);
    }

    [Fact]
    public void ControllerErrors_AreShownInStatus()
    {
        view.Load();
        view.Connect();
        Ptz.FailWith = new NotSupportedException("no zoom over USB");

        view.Zoom(Motion.Positive);

        Assert.True(view.StatusIsError);
        Assert.Equal("no zoom over USB", view.Status);
    }

    [Fact]
    public void AsyncFaults_AreShownInStatus()
    {
        view.Load();
        view.Connect();

        Ptz.RaiseFault(new TimeoutException("camera not answering"));

        Assert.Equal("camera not answering", view.Status);
    }

    [Fact]
    public void AddPreset_StoresOnCameraAndPersists()
    {
        view.Load();
        view.Connect();
        view.PromptAnswer = "Pulpit";

        view.AddPreset();

        Assert.Equal("Store 1", Assert.Single(Ptz.Calls));
        Assert.Equal("Pulpit", Assert.Single(view.Presets).Name);
        Assert.Single(presetStore.Saved!.Items);
    }

    [Fact]
    public void AddPreset_RequiresConnection()
    {
        view.Load();
        view.PromptAnswer = "Pulpit";

        view.AddPreset();

        Assert.True(view.StatusIsError);
        Assert.Empty(view.Presets);
    }

    [Fact]
    public void AddPreset_IsRolledBackWhenCameraFails()
    {
        view.Load();
        view.Connect();
        view.PromptAnswer = "Pulpit";
        Ptz.FailWith = new InvalidOperationException("no position");

        view.AddPreset();

        Assert.Empty(view.Presets);
        Assert.Null(presetStore.Saved);
    }

    [Fact]
    public void RecallPresetAt_UsesListOrder()
    {
        var book = new PresetBook();
        book.Add("A");
        book.Add("B");
        presetStore.Save(book);
        view.Load();
        view.Connect();

        view.RecallPresetAt(1);
        view.RecallPresetAt(5);

        Assert.Equal("Recall 2", Assert.Single(Ptz.Calls));
        Assert.True(view.StatusIsError);
    }

    [Fact]
    public void DeletePreset_ForgetsOnCameraWhenConfirmed()
    {
        var book = new PresetBook();
        book.Add("A");
        presetStore.Save(book);
        view.Load();
        view.Connect();

        view.DeletePreset(view.Presets[0]);

        Assert.Equal("Forget 1", Assert.Single(Ptz.Calls));
        Assert.Empty(view.Presets);
    }

    [Fact]
    public void DeletePreset_DoesNothingWhenCancelled()
    {
        var book = new PresetBook();
        book.Add("A");
        presetStore.Save(book);
        view.Load();
        view.ConfirmAnswer = false;

        view.DeletePreset(view.Presets[0]);

        Assert.Single(view.Presets);
    }

    [Fact]
    public void Close_PersistsViewState()
    {
        view.Load();
        view.ConnectionOptions = new ConnectionOptions { Mode = ControlMode.Visca, PortName = "COM3" };
        view.Speed = 0.8;
        view.AlwaysOnTop = true;

        view.Close();

        var saved = settingsStore.Saved!;
        Assert.Equal("COM3", saved.Connection.PortName);
        Assert.Equal(0.8, saved.Speed);
        Assert.True(saved.AlwaysOnTop);
        Assert.False(saved.AutoConnect);
    }
}
