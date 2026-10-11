using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using AppKit;
using AVFoundation;
using Foundation;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Session;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Storage;
using TenveoPtz.Core.Updates;
using TenveoPtz.Mac.Infrastructure.Serial;
using TenveoPtz.Mac.Infrastructure.Timing;
using TenveoPtz.Mac.Infrastructure.Updates;
using TenveoPtz.Mac.Infrastructure.Video;
using TenveoPtz.Mac.Views;

namespace TenveoPtz.Mac;

/// <summary>Composition root: wires the concrete macOS services into the presenter.</summary>
internal sealed class AppDelegate : NSApplicationDelegate
{
    private const string DataFolderName = "TenveoPTZ";
    private const string Repository = "bzzdevlabs/visca-cam-app";

    /// <summary>Log entries kept in memory (for diagnostics, as on Windows).</summary>
    private const int RecentLogEntries = 5000;

    private readonly string[] args;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(2) };
    private Logger log = Logger.Null;
    private MainWindow? window;
    private MainPresenter? presenter;

    public AppDelegate(string[] args)
    {
        this.args = args;
    }

    public override void DidFinishLaunching(NSNotification notification)
    {
        // Logs go where macOS apps keep theirs: ~/Library/Logs/TenveoPTZ (shown by Console.app).
        var debug = Array.Exists(args, arg => string.Equals(arg, "--debug", StringComparison.OrdinalIgnoreCase));
        var logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", DataFolderName);
        log = new Logger(new LogHub(debug ? LogLevel.Debug : LogLevel.Info, RecentLogEntries, new FileLogSink(logFolder)), "App");
        log.Info($"Tenveo PTZ {ParseVersion()} starting on macOS {NSProcessInfo.ProcessInfo.OperatingSystemVersionString} ({System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}), arguments: {string.Join(" ", args)}");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => log.Error("Unhandled exception, the app is closing.", e.ExceptionObject as Exception);

        NSApplication.SharedApplication.MainMenu = MainMenu.Create();
        ApplyTheme(FindOption("--theme="));

        // Ask for the camera before the window may auto-connect, so the first connection works.
        if (AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video) == AVAuthorizationStatus.NotDetermined
            && AVFoundationVideoService.FindCaptureDevices().Length > 0)
        {
            AVCaptureDevice.RequestAccessForMediaType(AVAuthorizationMediaType.Video, _ => BeginInvokeOnMainThread(ShowMainWindow));
        }
        else
        {
            ShowMainWindow();
        }
    }

    public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender) => true;

    public override void WillTerminate(NSNotification notification)
    {
        window?.RaiseClosing();
        presenter?.Dispose();
        http.Dispose();
        if (window?.RestartRequested == true)
        {
            log.Info("Restarting after the update.");
            Relaunch();
        }

        log.Info("Exited.");
    }

    private void ShowMainWindow()
    {
        var installer = new DiskImageInstaller(NSBundle.MainBundle.BundlePath);
        installer.DeleteLeftovers();
        var version = ParseVersion();
        var dataFolder = Path.Combine(
            NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.ApplicationSupportDirectory, NSSearchPathDomain.User)[0].Path!,
            DataFolderName);

        window = new MainWindow();
        var videoService = new AVFoundationVideoService(window.VideoView);
        var serialPorts = new SerialPortProvider();
        presenter = new MainPresenter(
            window,
            videoService,
            serialPorts,
            new CameraSessionFactory(videoService, serialPorts, new RunLoopTickerFactory(), log.For("Session")),
            new XmlFileStore<AppSettings>(Path.Combine(dataFolder, "settings.xml"), log.For("Storage")),
            new XmlFileStore<PresetBook>(Path.Combine(dataFolder, "presets.xml"), log.For("Storage")),
            new GitHubUpdateService(http, Repository, version, installer, ReleasePlatform.MacOS),
            version,
            log.For("Main"));
        window.Present();

        if (FindOption("--screenshot=") is { } screenshot)
        {
            TakeScreenshotAndQuit(screenshot);
        }
    }

    /// <summary>Smoke test: lets the window settle, saves an image of it, then closes it like a user would.</summary>
    private void TakeScreenshotAndQuit(string path)
    {
        NSTimer.CreateScheduledTimer(TimeSpan.FromSeconds(2), _ =>
        {
            try
            {
                window!.SaveScreenshot(Path.GetFullPath(path));
                window.PerformClose(null);
            }
            catch (Exception ex)
            {
                log.Error("Smoke test screenshot failed.", ex);
                Console.Error.WriteLine(ex);
                Environment.Exit(1);
            }
        });
    }

    /// <summary>Starts the (updated) bundle again once this process has exited and released the camera.</summary>
    private static void Relaunch()
    {
        var start = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("while kill -0 \"$1\" 2>/dev/null; do sleep 0.2; done; exec /usr/bin/open \"$2\"");
        start.ArgumentList.Add("relaunch");
        start.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        start.ArgumentList.Add(NSBundle.MainBundle.BundlePath);
        Process.Start(start)?.Dispose();
    }

    /// <summary>Reads <c>--theme=light|dark</c> (used for screenshots); defaults to the system appearance.</summary>
    private static void ApplyTheme(string? theme)
    {
        var name = theme?.ToLowerInvariant() switch
        {
            "light" => NSAppearance.NameAqua,
            "dark" => NSAppearance.NameDarkAqua,
            _ => null,
        };
        if (name is not null)
        {
            NSApplication.SharedApplication.Appearance = NSAppearance.GetAppearance(name);
        }
    }

    /// <summary>
    /// Version used for the update check: the assembly's, unless <c>--simulate-version=x.y.z</c>
    /// is given (to exercise the update with an older version).
    /// </summary>
    private Version ParseVersion()
    {
        var value = FindOption("--simulate-version=");
        return value is not null && Version.TryParse(value, out var version) ? version : typeof(AppDelegate).Assembly.GetName().Version!;
    }

    private string? FindOption(string prefix)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg.Substring(prefix.Length);
            }
        }

        return null;
    }
}
