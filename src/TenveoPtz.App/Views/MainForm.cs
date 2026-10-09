using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Video;

namespace TenveoPtz.App.Views;

/// <summary>
/// Main window: passive <see cref="IMainView"/> implementation. It forwards control events
/// to the presenter and renders what the presenter supplies. Layout and keyboard handling
/// live in the other partial files.
/// </summary>
internal sealed partial class MainForm : Form, IMainView
{
    public MainForm()
    {
        InitializeLayout();
        ForwardControlEvents();
    }

    public event EventHandler? ViewLoaded;

    public event EventHandler? ViewClosing;

    public event EventHandler? RefreshDevicesRequested;

    public event EventHandler? ConnectRequested;

    public event EventHandler? DisconnectRequested;

    public event EventHandler<PanTiltEventArgs>? PanTiltRequested;

    public event EventHandler<MotionEventArgs>? ZoomRequested;

    public event EventHandler<MotionEventArgs>? FocusRequested;

    public event EventHandler<ToggleEventArgs>? AutoFocusRequested;

    public event EventHandler? HomeRequested;

    public event EventHandler? AddPresetRequested;

    public event EventHandler<PresetEventArgs>? RecallPresetRequested;

    public event EventHandler<PresetIndexEventArgs>? RecallPresetAtRequested;

    public event EventHandler<PresetEventArgs>? OverwritePresetRequested;

    public event EventHandler<PresetEventArgs>? RenamePresetRequested;

    public event EventHandler<PresetEventArgs>? DeletePresetRequested;

    public event EventHandler<PresetMoveEventArgs>? MovePresetRequested;

    /// <summary>Area hosting the DirectShow video window.</summary>
    public Control VideoHost => videoSurface;

    public ConnectionOptions ConnectionOptions
    {
        get => connectionBar.Options;
        set => connectionBar.Options = value;
    }

    public double Speed
    {
        get => movePanel.Speed;
        set => movePanel.Speed = value;
    }

    public bool AlwaysOnTop
    {
        get => connectionBar.AlwaysOnTop;
        set => connectionBar.AlwaysOnTop = value;
    }

    public void ShowVideoDevices(IReadOnlyList<VideoDevice> devices) => connectionBar.ShowDevices(devices);

    public void ShowSerialPorts(IReadOnlyList<string> portNames) => connectionBar.ShowPorts(portNames);

    public void ShowConnected(bool connected)
    {
        connectionBar.ShowConnected(connected);
        videoSurface.ShowPlaceholder = !connected;
    }

    public void ShowPresets(IReadOnlyList<Preset> presets, Preset? selected) => presetPanel.ShowPresets(presets, selected);

    public void ShowStatus(string message, bool isError) => statusLine.Show(message, isError);

    public string? PromptText(string title, string prompt, string initialValue) => FluentDialog.Prompt(this, title, prompt, initialValue);

    public bool Confirm(string message) => FluentDialog.Confirm(this, "Are you sure?", message, "Yes");

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowChrome.Apply(this, Theme.Current);
        Theme.StartWatching();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
            Theme.StopWatching();
            toolTips.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ViewLoaded?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel)
        {
            ViewClosing?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ForwardControlEvents()
    {
        connectionBar.RefreshRequested += (_, e) => RefreshDevicesRequested?.Invoke(this, e);
        connectionBar.ConnectRequested += (_, e) => ConnectRequested?.Invoke(this, e);
        connectionBar.DisconnectRequested += (_, e) => DisconnectRequested?.Invoke(this, e);
        connectionBar.AlwaysOnTopChanged += (_, _) => TopMost = connectionBar.AlwaysOnTop;

        movePanel.PanTiltRequested += (_, e) => PanTiltRequested?.Invoke(this, e);
        movePanel.HomeRequested += (_, e) => HomeRequested?.Invoke(this, e);
        lensPanel.ZoomRequested += (_, e) => ZoomRequested?.Invoke(this, e);
        lensPanel.FocusRequested += (_, e) => FocusRequested?.Invoke(this, e);
        lensPanel.AutoFocusRequested += (_, e) => AutoFocusRequested?.Invoke(this, e);

        presetPanel.AddRequested += (_, e) => AddPresetRequested?.Invoke(this, e);
        presetPanel.RecallRequested += (_, e) => RecallPresetRequested?.Invoke(this, e);
        presetPanel.OverwriteRequested += (_, e) => OverwritePresetRequested?.Invoke(this, e);
        presetPanel.RenameRequested += (_, e) => RenamePresetRequested?.Invoke(this, e);
        presetPanel.DeleteRequested += (_, e) => DeletePresetRequested?.Invoke(this, e);
        presetPanel.MoveRequested += (_, e) => MovePresetRequested?.Invoke(this, e);
    }
}
