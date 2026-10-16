using System;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Tests.Fakes;

namespace TenveoPtz.Core.Tests.Presentation;

public sealed class SettingsPanelTests : IDisposable
{
    private readonly FakeView view = new();
    private readonly FakeSessionFactory sessions = new();
    private readonly MemoryStore<AppSettings> settingsStore = new();
    private readonly MemoryStore<PresetBook> presetStore = new();
    private readonly MainPresenter presenter;

    public SettingsPanelTests()
    {
        var devices = new FakeDevices();
        presenter = new MainPresenter(view, devices, devices, sessions, settingsStore, presetStore, log: new Logger(new LogHub(LogLevel.Info, 10), "Test"));
    }

    public void Dispose() => presenter.Dispose();

    [Fact]
    public void Load_AppliesTheSavedTheme()
    {
        settingsStore.Save(new AppSettings { Theme = AppTheme.Dark });

        view.Load();

        Assert.Equal(AppTheme.Dark, view.Theme);
    }

    [Fact]
    public void Panel_ShowsTheCurrentStateOfTheWindow()
    {
        view.Load();
        view.AlwaysOnTop = true;
        view.BackgroundNumpad = true;
        view.Speed = 0.7;

        view.OpenSettings();

        Assert.True(view.SettingsShown!.AlwaysOnTop);
        Assert.True(view.SettingsShown.BackgroundNumpad);
        Assert.Equal(0.7, view.SettingsShown.Speed);
    }

    [Fact]
    public void Save_AppliesAndPersistsTheSettings()
    {
        view.Load();
        view.SettingsAnswer = edited =>
        {
            edited.InvertPan = true;
            edited.CheckForUpdates = false;
            edited.AlwaysOnTop = true;
            edited.BackgroundNumpad = true;
            edited.Theme = AppTheme.Light;
            return SettingsEdit.Save(edited);
        };

        view.OpenSettings();

        var saved = settingsStore.Saved!;
        Assert.True(saved.InvertPan);
        Assert.False(saved.CheckForUpdates);
        Assert.True(view.AlwaysOnTop);
        Assert.True(view.BackgroundNumpad);
        Assert.Equal(AppTheme.Light, view.Theme);
        Assert.Equal("Settings saved.", view.Status);
    }

    [Fact]
    public void Save_InvertSettingsApplyToTheNextMove()
    {
        view.Load();
        view.Connect();
        view.SettingsAnswer = edited =>
        {
            edited.InvertPan = true;
            return SettingsEdit.Save(edited);
        };
        view.OpenSettings();

        view.PanTilt(Motion.Positive, Motion.None);

        Assert.Equal("Move Positive None", Ptz.Calls[Ptz.Calls.Count - 1]);
    }

    [Fact]
    public void Cancel_ChangesNothing()
    {
        view.Load();
        view.SettingsAnswer = edited =>
        {
            edited.InvertTilt = true;
            return null;
        };

        view.OpenSettings();
        view.Close();

        Assert.False(settingsStore.Saved!.InvertTilt);
    }

    [Fact]
    public void ChangingUvcMoveMode_ReopensTheCamera()
    {
        settingsStore.Save(new AppSettings { Connection = new ConnectionOptions { Mode = ControlMode.Uvc } });
        view.Load();
        view.Connect();
        var opened = sessions.OpenCount;
        view.SettingsAnswer = edited =>
        {
            edited.UvcContinuousMoves = true;
            return SettingsEdit.Save(edited);
        };

        view.OpenSettings();

        Assert.Equal(opened + 1, sessions.OpenCount);
        Assert.True(sessions.LastOptions!.UvcContinuousMoves);
        Assert.True(view.Connected);
    }

    [Fact]
    public void Reset_RestoresDefaultsKeepsPresetsAndDisconnects()
    {
        var book = new PresetBook();
        book.Add("Lectern");
        presetStore.Save(book);
        settingsStore.Save(new AppSettings
        {
            Connection = new ConnectionOptions { Mode = ControlMode.Visca, PortName = "COM3" },
            Speed = 0.9,
            InvertPan = true,
            AlwaysOnTop = true,
            BackgroundNumpad = true,
            Theme = AppTheme.Dark,
            CheckForUpdates = false,
        });
        view.Load();
        view.Connect();
        view.SettingsAnswer = _ => SettingsEdit.Reset();

        view.OpenSettings();

        var saved = settingsStore.Saved!;
        Assert.False(view.Connected);
        Assert.Contains("Dispose", Ptz.Calls);
        Assert.Equal(ControlMode.Uvc, saved.Connection.Mode);
        Assert.Equal(0.5, saved.Speed);
        Assert.False(saved.InvertPan);
        Assert.True(saved.CheckForUpdates);
        Assert.Equal(AppTheme.System, view.Theme);
        Assert.False(view.AlwaysOnTop);
        Assert.False(view.BackgroundNumpad);
        Assert.Equal(0.5, view.Speed);
        Assert.Equal(ControlMode.Uvc, view.ConnectionOptions.Mode);
        Assert.Single(presetStore.Saved?.Items ?? book.Items);
    }

    [Fact]
    public void Reset_AsksFirst()
    {
        settingsStore.Save(new AppSettings { InvertPan = true });
        view.Load();
        view.ConfirmAnswer = false;
        view.SettingsAnswer = _ => SettingsEdit.Reset();

        view.OpenSettings();

        Assert.True(settingsStore.Saved!.InvertPan);
    }

    private RecordingPtzController Ptz => sessions.Ptz;
}
