using System;
using System.Collections.Generic;
using System.Threading;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Tests.Fakes;

namespace TenveoPtz.Core.Tests.Presentation;

/// <summary>Lens and home buttons, raw VISCA commands, device listing and faults raised off the UI thread.</summary>
public sealed class CameraCommandsTests : IDisposable
{
    private readonly FakeView view = new();
    private readonly FakeDevices devices = new();
    private readonly FakeSessionFactory sessions = new();
    private readonly MemoryStore<AppSettings> settingsStore = new();
    private readonly MemoryStore<PresetBook> presetStore = new();
    private readonly MainPresenter presenter;

    public CameraCommandsTests()
    {
        presenter = new MainPresenter(view, devices, devices, sessions, settingsStore, presetStore, log: new Logger(new LogHub(LogLevel.Debug, 50), "Test"));
    }

    public void Dispose() => presenter.Dispose();

    [Fact]
    public void FocusButtons_DriveTheFocus()
    {
        view.Load();
        view.Connect();

        view.Focus(Motion.Positive);
        view.Focus(Motion.None);

        Assert.Equal(new[] { "Focus Positive", "Focus None" }, sessions.Ptz.Calls);
    }

    [Fact]
    public void AutofocusSwitch_IsSentToTheCamera()
    {
        view.Load();
        view.Connect();

        view.AutoFocus(false);
        view.AutoFocus(true);

        Assert.Equal(new[] { "AutoFocus False", "AutoFocus True" }, sessions.Ptz.Calls);
    }

    [Fact]
    public void HomeButton_SendsTheCameraHome()
    {
        view.Load();
        view.Connect();

        view.Home();

        Assert.Equal(new[] { "Home" }, sessions.Ptz.Calls);
    }

    [Fact]
    public void Zoom_WhileHeld_FollowsSpeedChanges()
    {
        view.Load();
        view.Connect();
        view.Zoom(Motion.Positive);

        view.ChangeSpeed(0.9);

        Assert.Equal(new[] { "Zoom Positive", "Zoom Positive" }, sessions.Ptz.Calls);
        Assert.Equal(0.9, sessions.Ptz.LastSpeed);
    }

    [Fact]
    public void RawCommand_IsSentOverVisca()
    {
        sessions.Ptz = new RawRecordingPtzController();
        view.Load();
        view.Connect();

        view.SendRaw("81 01 06 04 FF");

        Assert.Equal(new[] { "Raw 81 01 06 04 FF" }, sessions.Ptz.Calls);
        Assert.False(view.StatusIsError);
        Assert.Equal("Sent raw command 81 01 06 04 FF.", view.Status);
    }

    [Fact]
    public void DeviceListingFailure_IsShownAsError()
    {
        view.Load();
        devices.FailWith = new InvalidOperationException("driver crashed");

        view.Refresh();

        Assert.True(view.StatusIsError);
        Assert.Equal("Could not list devices: driver crashed", view.Status);
    }

    [Fact]
    public void Refresh_KeepsTheChosenOptions()
    {
        view.Load();
        view.ConnectionOptions = new ConnectionOptions { Mode = ControlMode.Visca, PortName = "COM3", Address = 2 };

        view.Refresh();

        Assert.Equal(ControlMode.Visca, view.ConnectionOptions.Mode);
        Assert.Equal(2, view.ConnectionOptions.Address);
    }

    [Fact]
    public void CameraFaults_FromABackgroundThreadAreShownOnTheUiThread()
    {
        var ui = new QueuedContext();
        var window = new FakeView();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(ui);
        MainPresenter onUiThread;
        try
        {
            onUiThread = new MainPresenter(window, devices, devices, sessions, settingsStore, presetStore);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        using (onUiThread)
        {
            window.Load();
            window.Connect();
            sessions.Ptz.RaiseFault(new InvalidOperationException("serial cable unplugged"));

            Assert.NotEqual("serial cable unplugged", window.Status);
            ui.RunAll();
            Assert.True(window.StatusIsError);
            Assert.Equal("serial cable unplugged", window.Status);
        }
    }

    /// <summary>UI thread stand-in: posted callbacks wait until <see cref="RunAll"/>.</summary>
    private sealed class QueuedContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> queue = new();

        public override void Post(SendOrPostCallback d, object? state) => queue.Enqueue((d, state));

        public void RunAll()
        {
            while (queue.Count > 0)
            {
                var (callback, state) = queue.Dequeue();
                callback(state);
            }
        }
    }
}
