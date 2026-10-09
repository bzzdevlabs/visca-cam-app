using System;
using System.IO;
using System.Threading;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Session;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Storage;
using TenveoPtz.Core.Transport;
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
    private readonly SynchronizationContext? uiContext;

    private AppSettings settings = new();
    private PresetBook presets = new();
    private CameraSession? session;

    public MainPresenter(
        IMainView view,
        IVideoService videoService,
        ISerialPortProvider serialPorts,
        ICameraSessionFactory sessionFactory,
        IStore<AppSettings> settingsStore,
        IStore<PresetBook> presetStore)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.videoService = videoService ?? throw new ArgumentNullException(nameof(videoService));
        this.serialPorts = serialPorts ?? throw new ArgumentNullException(nameof(serialPorts));
        this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        this.presetStore = presetStore ?? throw new ArgumentNullException(nameof(presetStore));
        uiContext = SynchronizationContext.Current;

        view.ViewLoaded += (_, _) => Initialize();
        view.ViewClosing += (_, _) => Shutdown();
        view.RefreshDevicesRequested += (_, _) => RefreshDevices();
        view.ConnectRequested += (_, _) => Connect();
        view.DisconnectRequested += (_, _) => Disconnect();
        SubscribeCameraEvents();
        SubscribePresetEvents();
    }

    public bool IsConnected => session is not null;

    public void Dispose() => CloseSession();

    private void Initialize()
    {
        settings = settingsStore.Load();
        presets = presetStore.Load();

        view.AlwaysOnTop = settings.AlwaysOnTop;
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
            view.ShowStatus("Choose the camera and how to control it, then press Connect.", false);
        }
    }

    private void Shutdown()
    {
        settings.Connection = view.ConnectionOptions.Clone();
        settings.Speed = view.Speed;
        settings.AlwaysOnTop = view.AlwaysOnTop;
        settings.AutoConnect = IsConnected;
        TrySave(() => settingsStore.Save(settings));
        CloseSession();
    }

    private void RefreshDevices()
    {
        var options = view.ConnectionOptions;
        try
        {
            view.ShowVideoDevices(videoService.GetDevices());
            view.ShowSerialPorts(serialPorts.GetPortNames());
            view.ConnectionOptions = options;
        }
        catch (Exception ex)
        {
            view.ShowStatus("Could not list devices: " + ex.Message, true);
        }
    }

    private void Connect()
    {
        CloseSession();
        var options = view.ConnectionOptions.Clone();
        try
        {
            session = sessionFactory.Open(options);
            session.Ptz.Faulted += OnPtzFaulted;
        }
        catch (Exception ex)
        {
            view.ShowConnected(false);
            view.ShowStatus("Connection failed: " + ex.Message, true);
            return;
        }

        settings.Connection = options;
        settings.AutoConnect = true;
        TrySave(() => settingsStore.Save(settings));
        view.ShowConnected(true);
        view.ShowStatus("Connected: " + session.Description, false);
    }

    private void Disconnect()
    {
        CloseSession();
        settings.AutoConnect = false;
        TrySave(() => settingsStore.Save(settings));

        view.ShowConnected(false);
        view.ShowStatus("Disconnected.", false);
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
    }

    private void OnPtzFaulted(object? sender, ErrorEventArgs e)
    {
        var message = e.GetException().Message;
        RunOnUi(() => view.ShowStatus(message, true));
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
            view.ShowStatus("Could not save settings: " + ex.Message, true);
        }
    }
}
