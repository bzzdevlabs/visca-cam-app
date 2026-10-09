using System;
using System.Collections.Generic;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Video;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>Scriptable view: tests raise user intents and inspect what the presenter displayed.</summary>
internal sealed class FakeView : IMainView
{
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

    public ConnectionOptions ConnectionOptions { get; set; } = new();

    public double Speed { get; set; }

    public bool AlwaysOnTop { get; set; }

    public bool Connected { get; private set; }

    public IReadOnlyList<Preset> Presets { get; private set; } = Array.Empty<Preset>();

    public string Status { get; private set; } = string.Empty;

    public bool StatusIsError { get; private set; }

    public string? PromptAnswer { get; set; }

    public bool ConfirmAnswer { get; set; } = true;

    public void ShowVideoDevices(IReadOnlyList<VideoDevice> devices)
    {
    }

    public void ShowSerialPorts(IReadOnlyList<string> portNames)
    {
    }

    public void ShowConnected(bool connected) => Connected = connected;

    public void ShowPresets(IReadOnlyList<Preset> presets, Preset? selected) => Presets = new List<Preset>(presets);

    public void ShowStatus(string message, bool isError)
    {
        Status = message;
        StatusIsError = isError;
    }

    public string? PromptText(string title, string prompt, string initialValue) => PromptAnswer;

    public bool Confirm(string message) => ConfirmAnswer;

    public void Load() => ViewLoaded?.Invoke(this, EventArgs.Empty);

    public void Close() => ViewClosing?.Invoke(this, EventArgs.Empty);

    public void Refresh() => RefreshDevicesRequested?.Invoke(this, EventArgs.Empty);

    public void Connect() => ConnectRequested?.Invoke(this, EventArgs.Empty);

    public void Disconnect() => DisconnectRequested?.Invoke(this, EventArgs.Empty);

    public void PanTilt(Motion pan, Motion tilt) => PanTiltRequested?.Invoke(this, new PanTiltEventArgs(pan, tilt));

    public void Zoom(Motion direction) => ZoomRequested?.Invoke(this, new MotionEventArgs(direction));

    public void Focus(Motion direction) => FocusRequested?.Invoke(this, new MotionEventArgs(direction));

    public void AutoFocus(bool enabled) => AutoFocusRequested?.Invoke(this, new ToggleEventArgs(enabled));

    public void Home() => HomeRequested?.Invoke(this, EventArgs.Empty);

    public void AddPreset() => AddPresetRequested?.Invoke(this, EventArgs.Empty);

    public void RecallPreset(Preset preset) => RecallPresetRequested?.Invoke(this, new PresetEventArgs(preset));

    public void RecallPresetAt(int index) => RecallPresetAtRequested?.Invoke(this, new PresetIndexEventArgs(index));

    public void OverwritePreset(Preset preset) => OverwritePresetRequested?.Invoke(this, new PresetEventArgs(preset));

    public void RenamePreset(Preset preset) => RenamePresetRequested?.Invoke(this, new PresetEventArgs(preset));

    public void DeletePreset(Preset preset) => DeletePresetRequested?.Invoke(this, new PresetEventArgs(preset));

    public void MovePreset(Preset preset, int offset) => MovePresetRequested?.Invoke(this, new PresetMoveEventArgs(preset, offset));
}
