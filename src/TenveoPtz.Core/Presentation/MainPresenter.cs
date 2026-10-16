using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Session;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Storage;
using TenveoPtz.Core.Transport;
using TenveoPtz.Core.Updates;
using TenveoPtz.Core.Video;

namespace TenveoPtz.Core.Presentation;

/// <summary>
/// Presenter of the main window (Model-View-Presenter): reacts to view events, drives the
/// camera session and keeps settings and presets persisted. This part handles lifecycle and connection;
/// camera motion and presets live in the other partial files.
/// </summary>
public sealed partial class MainPresenter : IDisposable
{
    private readonly IMainView view;
    private readonly IVideoService videoService;
    private readonly ISerialPortProvider serialPorts;
    private readonly ICameraSessionFactory sessionFactory;
    private readonly IStore<AppSettings> settingsStore;
    private readonly IStore<PresetBook> presetStore;
    private readonly IUpdateService? updates;
    private readonly Version currentVersion;
    private readonly SynchronizationContext? uiContext;
    private readonly Logger log;

    private AppSettings settings = new();
    private PresetBook presets = new();
    private CameraSession? session;

    public MainPresenter(
        IMainView view,
        IVideoService videoService,
        ISerialPortProvider serialPorts,
        ICameraSessionFactory sessionFactory,
        IStore<AppSettings> settingsStore,
        IStore<PresetBook> presetStore,
        IUpdateService? updates = null,
        Version? currentVersion = null,
        Logger? log = null)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.videoService = videoService ?? throw new ArgumentNullException(nameof(videoService));
        this.serialPorts = serialPorts ?? throw new ArgumentNullException(nameof(serialPorts));
        this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        this.presetStore = presetStore ?? throw new ArgumentNullException(nameof(presetStore));
        this.updates = updates;
        this.currentVersion = currentVersion ?? new Version(0, 0);
        uiContext = SynchronizationContext.Current;
        this.log = log ?? Logger.Null;

        view.ViewLoaded += (_, _) => Initialize();
        view.ViewShown += (_, _) => CheckForUpdates();
        view.ViewClosing += (_, _) => Shutdown();
        view.RefreshDevicesRequested += (_, _) => RefreshDevices();
        view.ConnectRequested += (_, _) => Connect();
        view.DisconnectRequested += (_, _) => Disconnect();
        view.PreviewChanged += (_, _) => ChangePreview();
        view.SettingsRequested += (_, _) => EditSettings();
        SubscribeCameraEvents();
        SubscribePresetEvents();
    }

    public bool IsConnected => session is not null;

    public void Dispose() => CloseSession();

    private void Initialize()
    {
        settings = settingsStore.Load();
        presets = presetStore.Load();
        log.Info($"Loaded settings ({settings.Connection.Mode} mode, auto-connect {(settings.AutoConnect ? "on" : "off")}) and {presets.Items.Count} preset(s).");

        view.AlwaysOnTop = settings.AlwaysOnTop;
        view.BackgroundNumpad = settings.BackgroundNumpad;
        view.Theme = settings.Theme;
        view.Speed = settings.Speed;
        RefreshDevices();
        view.ConnectionOptions = settings.Connection.Clone();
        view.ShowPresets(presets.Items, null);
        view.ShowConnected(false);

        if (settings.AutoConnect)
        {
            Connect();
        }
        else
        {
            ShowStatus(Strings.ChooseCameraThenConnect, false);
        }
    }

    private void Shutdown()
    {
        closing = true;
        ReadViewState();
        settings.AutoConnect = IsConnected;
        TrySave(() => settingsStore.Save(settings));
        CloseSession();
        log.Info("Closed.");
    }

    private void RefreshDevices()
    {
        var options = view.ConnectionOptions;
        try
        {
            var devices = videoService.GetDevices();
            var ports = serialPorts.GetPortNames();
            log.Info($"Video devices: {Describe(devices.Select(d => d.Name))}. Serial ports: {Describe(ports)}.");
            view.ShowVideoDevices(devices);
            view.ShowSerialPorts(ports);
            view.ConnectionOptions = options;
        }
        catch (Exception ex)
        {
            ShowStatus(Strings.CouldNotListDevices(ex.Message), true, ex);
        }
    }

    /// <summary>Toggling the preview while connected reopens the camera with the new setting.</summary>
    private void ChangePreview()
    {
        if (IsConnected)
        {
            Connect();
        }
    }

    private void Connect()
    {
        CloseSession();
        var options = view.ConnectionOptions.Clone();
        options.UvcContinuousMoves = settings.UvcContinuousMoves;
        log.Info($"Connecting: mode {options.Mode}, video \"{options.VideoDeviceName}\" (preview {(options.ShowPreview ? "on" : "off")}), port {options.PortName}, {options.BaudRate} baud, address {options.Address}.");
        try
        {
            session = sessionFactory.Open(options);
            session.Ptz.Faulted += OnPtzFaulted;
        }
        catch (Exception ex)
        {
            view.ShowConnected(false);
            ShowStatus(Strings.ConnectionFailed(ex.Message), true, ex);
            return;
        }

        settings.Connection = options;
        settings.AutoConnect = true;
        TrySave(() => settingsStore.Save(settings));
        view.ShowConnected(true);
        ShowStatus(Strings.Connected(session.Description, options.ShowPreview), false);
    }

    private void Disconnect()
    {
        CloseSession();
        settings.AutoConnect = false;
        TrySave(() => settingsStore.Save(settings));

        view.ShowConnected(false);
        ShowStatus(Strings.Disconnected, false);
    }

    private void CloseSession()
    {
        if (session is null)
        {
            return;
        }

        session.Ptz.Faulted -= OnPtzFaulted;
        session.Dispose();
        session = null;
        heldPan = heldTilt = heldZoom = Motion.None;
    }

    private void OnPtzFaulted(object? sender, ErrorEventArgs e)
    {
        var exception = e.GetException();
        RunOnUi(() => ShowStatus(exception.Message, true, exception));
    }

    /// <summary>Shows a status message and records it, so the log tells what the user saw.</summary>
    private void ShowStatus(string message, bool isError, Exception? exception = null)
    {
        if (exception is not null)
        {
            log.Error(message, exception);
        }
        else if (isError)
        {
            log.Warning(message);
        }
        else
        {
            log.Info(message);
        }

        view.ShowStatus(message, isError);
    }

    private static string Describe(IEnumerable<string> names)
    {
        var list = string.Join(", ", names);
        return list.Length == 0 ? "none" : list;
    }

    private void RunOnUi(Action action)
    {
        if (uiContext is null)
        {
            action();
        }
        else
        {
            uiContext.Post(_ => action(), null);
        }
    }

    private void TrySave(Action save)
    {
        try
        {
            save();
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
        {
            ShowStatus(Strings.CouldNotSaveSettings(ex.Message), true, ex);
        }
    }
}
