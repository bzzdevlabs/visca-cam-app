using System;
using System.Collections.Generic;
using System.IO;
using AppKit;
using Foundation;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Video;
using TenveoPtz.Mac.Views.Controls;

namespace TenveoPtz.Mac.Views;

/// <summary>
/// Main window: passive <see cref="IMainView"/> implementation. It forwards control events
/// to the presenter and renders what the presenter supplies. Layout and keyboard handling
/// live in the other partial files.
/// </summary>
internal sealed partial class MainWindow : NSWindow, IMainView
{
    private bool closingRaised;

    public MainWindow()
        : base(new CoreGraphics.CGRect(0, 0, 1120, 740), NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable | NSWindowStyle.Resizable, NSBackingStore.Buffered, false)
    {
        ReleaseWhenClosed(false);
        Title = "Tenveo PTZ";
        ContentMinSize = new CoreGraphics.CGSize(960, 640);
        InitializeLayout();
        WillClose += (_, _) => RaiseClosing();
    }

    public event EventHandler? ViewLoaded;

    public event EventHandler? ViewShown;

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

    /// <summary>Raised by the debug console, which only exists on Windows for now.</summary>
    public event EventHandler<RawCommandEventArgs>? RawCommandRequested
    {
        add { }
        remove { }
    }

    /// <summary>The window was closed by <see cref="Restart"/>: the application must start again.</summary>
    public bool RestartRequested { get; private set; }

    /// <summary>Area hosting the camera preview.</summary>
    public VideoView VideoView => videoView;

    public ConnectionOptions ConnectionOptions
    {
        get => new()
        {
            VideoDeviceName = Selected(devicePopup, videoDevices)?.Name ?? string.Empty,
            Mode = modePopup.IndexOfSelectedItem == 1 ? ControlMode.Visca : ControlMode.Uvc,
            PortName = Selected(portPopup, serialPorts) ?? string.Empty,
            BaudRate = BaudRates[Math.Max(0, (int)baudPopup.IndexOfSelectedItem)],
            Address = (int)addressPopup.IndexOfSelectedItem + 1,
        };
        set
        {
            devicePopup.SelectItem(Math.Max(0, videoDevices.FindIndex(d => d.Name == value.VideoDeviceName)));
            modePopup.SelectItem(value.Mode == ControlMode.Visca ? 1 : 0);
            portPopup.SelectItem(Math.Max(0, serialPorts.IndexOf(value.PortName)));
            var baud = Array.IndexOf(BaudRates, value.BaudRate);
            baudPopup.SelectItem(baud >= 0 ? baud : Array.IndexOf(BaudRates, ConnectionOptions.DefaultBaudRate));
            addressPopup.SelectItem(value.Address is >= 1 and <= 7 ? value.Address - 1 : 0);
            UpdateSerialSettings();
        }
    }

    public double Speed
    {
        get => (speedSlider.DoubleValue - speedSlider.MinValue) / (speedSlider.MaxValue - speedSlider.MinValue);
        set => speedSlider.DoubleValue = speedSlider.MinValue + Math.Round(Math.Max(0, Math.Min(1, value)) * (speedSlider.MaxValue - speedSlider.MinValue));
    }

    public bool AlwaysOnTop
    {
        get => pinButton.State == NSCellStateValue.On;
        set
        {
            pinButton.State = value ? NSCellStateValue.On : NSCellStateValue.Off;
            pinButton.Image = Symbol(value ? "pin.fill" : "pin", "Keep on top");
            Level = value ? NSWindowLevel.Floating : NSWindowLevel.Normal;
        }
    }

    public void ShowVideoDevices(IReadOnlyList<VideoDevice> devices)
    {
        videoDevices = new List<VideoDevice>(devices);
        Fill(devicePopup, videoDevices.ConvertAll(d => d.Name), "No camera found");
    }

    public void ShowSerialPorts(IReadOnlyList<string> portNames)
    {
        serialPorts = new List<string>(portNames);
        Fill(portPopup, serialPorts.ConvertAll(PortLabel), "No serial port");
    }

    public void ShowConnected(bool connected)
    {
        this.connected = connected;
        connectButton.Title = connected ? "Disconnect" : "Connect";
        connectButton.Image = Symbol(connected ? "stop.fill" : "play.fill", connectButton.Title);
        connectButton.BezelColor = connected ? null : NSColor.ControlAccent;
        foreach (var popup in new[] { devicePopup, modePopup, portPopup, baudPopup, addressPopup })
        {
            popup.Enabled = !connected;
        }

        videoView.ShowPlaceholder = !connected;
    }

    public void ShowPresets(IReadOnlyList<Preset> presets, Preset? selected)
    {
        presetTable.ShowPresets(presets, selected);
        emptyPresets.Hidden = presets.Count > 0;
    }

    public void ShowStatus(string message, bool isError)
    {
        statusLabel.StringValue = message;
        statusLabel.TextColor = isError ? NSColor.SystemRed : NSColor.SecondaryLabel;
    }

    public string? PromptText(string title, string prompt, string initialValue) => Dialogs.Prompt(title, prompt, initialValue);

    public bool Confirm(string message) => Dialogs.Confirm("Are you sure?", message, "Yes");

    public bool ConfirmUpdate(string message) => Dialogs.Confirm("Update available", message, "Update", "Not now");

    public void Restart()
    {
        RestartRequested = true;
        Close();
    }

    /// <summary>Shows the window: the presenter loads the settings first, then it may show dialogs over it.</summary>
    public void Present()
    {
        ViewLoaded?.Invoke(this, EventArgs.Empty);
        Center();
        MakeKeyAndOrderFront(null);
        if (OperatingSystem.IsMacOSVersionAtLeast(14))
        {
            NSApplication.SharedApplication.Activate();
        }
        else
        {
#pragma warning disable CA1422 // Replaced by Activate() on macOS 14, which does not exist before.
            NSApplication.SharedApplication.ActivateIgnoringOtherApps(true);
#pragma warning restore CA1422
        }
        BeginInvokeOnMainThread(() => ViewShown?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// Raises <see cref="ViewClosing"/> once: when the window closes, or when the application
    /// quits without closing it (Command-Q).
    /// </summary>
    public void RaiseClosing()
    {
        if (!closingRaised)
        {
            closingRaised = true;
            ViewClosing?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Saves an image of the window, title bar included, as PNG (used by the smoke test).</summary>
    public void SaveScreenshot(string path)
    {
        var view = ContentView!.Superview ?? ContentView;
        var image = view.BitmapImageRepForCachingDisplayInRect(view.Bounds)
            ?? throw new InvalidOperationException("Cannot capture the window.");
        view.CacheDisplay(view.Bounds, image);
        var png = image.RepresentationUsingTypeProperties(NSBitmapImageFileType.Png, new NSDictionary())
            ?? throw new InvalidOperationException("Cannot encode the screenshot.");
        File.WriteAllBytes(path, png.ToArray());
    }

    private static T? Selected<T>(NSPopUpButton popup, List<T> items) =>
        popup.IndexOfSelectedItem >= 0 && popup.IndexOfSelectedItem < items.Count ? items[(int)popup.IndexOfSelectedItem] : default;

    /// <summary>"/dev/cu.usbserial-1410" reads as "usbserial-1410".</summary>
    private static string PortLabel(string port) => port.StartsWith("/dev/cu.", StringComparison.Ordinal) ? port.Substring("/dev/cu.".Length) : port;

    private static void Fill(NSPopUpButton popup, List<string> labels, string emptyLabel)
    {
        popup.RemoveAllItems();
        if (labels.Count == 0)
        {
            popup.AddItem(emptyLabel);
            return;
        }

        foreach (var label in labels)
        {
            // AddItem would merge items with the same title (two identical cameras).
            popup.Menu!.AddItem(new NSMenuItem(label));
        }
    }

    /// <summary>Runs a control's handler, reporting unexpected errors instead of crashing the app.</summary>
    private static void Safely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Console.Error.WriteLine(ex);
            Dialogs.ShowError("Tenveo PTZ - unexpected error", ex.Message);
        }
    }

    private void ForwardControlEvents()
    {
        refreshButton.Activated += (_, _) => Safely(() => RefreshDevicesRequested?.Invoke(this, EventArgs.Empty));
        connectButton.Activated += (_, _) => Safely(() => (connected ? DisconnectRequested : ConnectRequested)?.Invoke(this, EventArgs.Empty));
        pinButton.Activated += (_, _) => AlwaysOnTop = pinButton.State == NSCellStateValue.On;
        modePopup.Activated += (_, _) => UpdateSerialSettings();

        directionPad.PanTiltRequested += (_, e) => Safely(() => PanTiltRequested?.Invoke(this, e));
        directionPad.HomeRequested += (_, e) => Safely(() => HomeRequested?.Invoke(this, e));
        zoomOut.Pressed += (_, _) => RaiseZoom(Core.Ptz.Motion.Negative);
        zoomIn.Pressed += (_, _) => RaiseZoom(Core.Ptz.Motion.Positive);
        zoomOut.Released += (_, _) => RaiseZoom(Core.Ptz.Motion.None);
        zoomIn.Released += (_, _) => RaiseZoom(Core.Ptz.Motion.None);
        focusNear.Pressed += (_, _) => RaiseFocus(Core.Ptz.Motion.Negative);
        focusFar.Pressed += (_, _) => RaiseFocus(Core.Ptz.Motion.Positive);
        focusNear.Released += (_, _) => RaiseFocus(Core.Ptz.Motion.None);
        focusFar.Released += (_, _) => RaiseFocus(Core.Ptz.Motion.None);
        autoFocus.Activated += (_, _) =>
        {
            UpdateFocusButtons();
            Safely(() => AutoFocusRequested?.Invoke(this, new ToggleEventArgs(autoFocus.State == NSCellStateValue.On)));
        };

        addPreset.Activated += (_, _) => Safely(() => AddPresetRequested?.Invoke(this, EventArgs.Empty));
        recallPreset.Activated += (_, _) => RaiseForSelection(RecallPresetRequested);
        overwritePreset.Activated += (_, _) => RaiseForSelection(OverwritePresetRequested);
        renamePreset.Activated += (_, _) => RaiseForSelection(RenamePresetRequested);
        deletePreset.Activated += (_, _) => RaiseForSelection(DeletePresetRequested);
        movePresetUp.Activated += (_, _) => RaiseMove(-1);
        movePresetDown.Activated += (_, _) => RaiseMove(1);
        presetTable.RecallRequested += (_, _) => RaiseForSelection(RecallPresetRequested);
        presetTable.DeleteRequested += (_, _) => RaiseForSelection(DeletePresetRequested);
    }

    private void RaiseZoom(Core.Ptz.Motion direction) => Safely(() => ZoomRequested?.Invoke(this, new MotionEventArgs(direction)));

    private void RaiseFocus(Core.Ptz.Motion direction) => Safely(() => FocusRequested?.Invoke(this, new MotionEventArgs(direction)));

    private void RaiseForSelection(EventHandler<PresetEventArgs>? handler)
    {
        if (presetTable.SelectedPreset is { } preset)
        {
            Safely(() => handler?.Invoke(this, new PresetEventArgs(preset)));
        }
    }

    private void RaiseMove(int offset)
    {
        if (presetTable.SelectedPreset is { } preset)
        {
            Safely(() => MovePresetRequested?.Invoke(this, new PresetMoveEventArgs(preset, offset)));
        }
    }

    private void UpdateSerialSettings() => serialRow.Hidden = modePopup.IndexOfSelectedItem != 1;

    private void UpdateFocusButtons()
    {
        focusNear.Enabled = autoFocus.State != NSCellStateValue.On;
        focusFar.Enabled = autoFocus.State != NSCellStateValue.On;
    }
}
