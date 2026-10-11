using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using TenveoPtz.App.Infrastructure.DirectShow;
using TenveoPtz.App.Infrastructure.Serial;
using TenveoPtz.App.Infrastructure.Timing;
using TenveoPtz.App.Views;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Session;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Storage;
using TenveoPtz.Core.Updates;

namespace TenveoPtz.App;

/// <summary>Composition root: wires the concrete Windows services into the presenter.</summary>
internal static class Program
{
    private const string DataFolderName = "TenveoPTZ";
    private const string Repository = "bzzdevlabs/visca-cam-app";

    /// <summary>Log entries kept in memory for the debug console and diagnostics.</summary>
    private const int RecentLogEntries = 5000;

    private static Logger log = Logger.Null;

    [STAThread]
    private static void Main(string[] args)
    {
        var debug = args.Any(arg => string.Equals(arg, "--debug", StringComparison.OrdinalIgnoreCase));
        var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DataFolderName);
        var logFolder = Path.Combine(dataFolder, "logs");
        var logHub = new LogHub(debug ? LogLevel.Debug : LogLevel.Info, RecentLogEntries, new FileLogSink(logFolder));
        log = new Logger(logHub, "App");
        log.Info($"Tenveo PTZ {Application.ProductVersion} starting on {Environment.OSVersion.VersionString}, CLR {Environment.Version}, arguments: {string.Join(" ", args)}");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.ThreadException += (_, e) => ShowFatal(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => log.Error("Unhandled exception, the app is closing.", e.ExceptionObject as Exception);

        Theme.Initialize(ParseTheme(args));
        var installer = new UpdateInstaller(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(Application.ExecutablePath));
        installer.DeleteLeftovers();

        var debugTools = new DebugTools(logHub, dataFolder, logFolder);
        if (Run(installer, ParseVersion(args), debugTools, debug))
        {
            // Started once the window is closed and the camera released, so the new instance can open it.
                log.Info("Restarting after the update.");
            Process.Start(Application.ExecutablePath);
        }

        log.Info("Exited.");
    }

    /// <summary>Shows the main window until it closes; returns whether an update asked for a restart.</summary>
    private static bool Run(UpdateInstaller installer, Version version, DebugTools debugTools, bool showDebugConsole)
    {
        var dataFolder = debugTools.DataFolder;

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var form = new MainForm(debugTools) { ShowDebugConsoleOnStart = showDebugConsole };
        var videoService = new DirectShowVideoService(form.VideoHost);
        var serialPorts = new SerialPortProvider();
        using var presenter = new MainPresenter(
            form,
            videoService,
            serialPorts,
            new CameraSessionFactory(videoService, serialPorts, new WinFormsTickerFactory(), log.For("Session")),
            new XmlFileStore<AppSettings>(Path.Combine(dataFolder, "settings.xml"), log.For("Storage")),
            new XmlFileStore<PresetBook>(Path.Combine(dataFolder, "presets.xml"), log.For("Storage")),
            new GitHubUpdateService(http, Repository, version, installer),
            version,
            log.For("Main"));

        Application.Run(form);
        return form.RestartRequested;
    }

    /// <summary>Reads <c>--theme=light|dark</c> (used for screenshots); defaults to the Windows setting.</summary>
    private static ThemeMode ParseTheme(string[] args)
    {
        var value = FindOption(args, "--theme=");
        return value is not null && Enum.TryParse<ThemeMode>(value, ignoreCase: true, out var mode) ? mode : ThemeMode.System;
    }

    /// <summary>
    /// Version used for the update check: the assembly's, unless <c>--simulate-version=x.y.z</c>
    /// is given (used by the smoke test to exercise the update with an older version).
    /// </summary>
    private static Version ParseVersion(string[] args)
    {
        var value = FindOption(args, "--simulate-version=");
        return value is not null && Version.TryParse(value, out var version) ? version : typeof(Program).Assembly.GetName().Version;
    }

    private static string? FindOption(string[] args, string prefix)
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

    private static void ShowFatal(Exception exception)
    {
        log.Error("Unexpected error.", exception);
        MessageBox.Show(
            exception.Message + Environment.NewLine + Environment.NewLine + "Details were written to the log (Ctrl+Shift+D opens the debug console).",
            "Tenveo PTZ - unexpected error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
