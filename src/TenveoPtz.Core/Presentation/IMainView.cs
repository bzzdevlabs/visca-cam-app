using System;
using System.Collections.Generic;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Video;

namespace TenveoPtz.Core.Presentation;

/// <summary>
/// Passive view of the main window (Model-View-Presenter). The view only reports user intent
/// through events and displays what the presenter tells it; it holds no application logic.
/// All members are called on the UI thread.
/// </summary>
public interface IMainView
{
    /// <summary>The window is shown for the first time.</summary>
    event EventHandler? ViewLoaded;

    /// <summary>The window is visible for the first time: dialogs can now be shown over it.</summary>
    event EventHandler? ViewShown;

    /// <summary>The window is closing; last chance to save state.</summary>
    event EventHandler? ViewClosing;

    event EventHandler? RefreshDevicesRequested;

    event EventHandler? ConnectRequested;

    event EventHandler? DisconnectRequested;

    /// <summary>Pan/tilt intent changed. Both axes <see cref="Ptz.Motion.None"/> means stop.</summary>
    event EventHandler<PanTiltEventArgs>? PanTiltRequested;

    event EventHandler<MotionEventArgs>? ZoomRequested;

    /// <summary>The speed slider moved; <see cref="Speed"/> holds the new value.</summary>
    event EventHandler? SpeedChanged;

    event EventHandler<MotionEventArgs>? FocusRequested;

    event EventHandler<ToggleEventArgs>? AutoFocusRequested;

    event EventHandler? HomeRequested;

    event EventHandler? AddPresetRequested;

    event EventHandler<PresetEventArgs>? RecallPresetRequested;

    event EventHandler<PresetIndexEventArgs>? RecallPresetAtRequested;

    event EventHandler<PresetEventArgs>? OverwritePresetRequested;

    event EventHandler<PresetEventArgs>? RenamePresetRequested;

    event EventHandler<PresetEventArgs>? DeletePresetRequested;

    event EventHandler<PresetMoveEventArgs>? MovePresetRequested;

    /// <summary>Protocol bytes typed in the hidden debug console, to send to the camera as they are.</summary>
    event EventHandler<RawCommandEventArgs>? RawCommandRequested;

    ConnectionOptions ConnectionOptions { get; set; }

    /// <summary>Relative movement speed, 0..1.</summary>
    double Speed { get; set; }

    bool AlwaysOnTop { get; set; }

    void ShowVideoDevices(IReadOnlyList<VideoDevice> devices);

    void ShowSerialPorts(IReadOnlyList<string> portNames);

    void ShowConnected(bool connected);

    void ShowPresets(IReadOnlyList<Preset> presets, Preset? selected);

    void ShowStatus(string message, bool isError);

    /// <summary>Asks the user for a line of text; returns <c>null</c> when cancelled.</summary>
    string? PromptText(string title, string prompt, string initialValue);

    bool Confirm(string message);

    /// <summary>Asks whether to install an available update.</summary>
    bool ConfirmUpdate(string message);

    /// <summary>Closes the window and starts the application again (to run an installed update).</summary>
    void Restart();
}
