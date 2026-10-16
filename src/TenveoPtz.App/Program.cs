using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using TenveoPtz.App.Infrastructure.DirectShow;
using TenveoPtz.App.Infrastructure.Serial;
using TenveoPtz.App.Infrastructure.Simulation;
using TenveoPtz.App.Infrastructure.Timing;
using TenveoPtz.App.Views;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Session;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Simulation;
using TenveoPtz.Core.Storage;
using TenveoPtz.Core.Updates;
using TenveoPtz.Core.Video;

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

        // The windows are built in this language, so it is read before they are created.
        var settingsStore = new XmlFileStore<AppSettings>(Path.Combine(dataFolder, "settings.xml"), log.For("Storage"));
        Strings.Language = ParseLanguage(args) ?? settingsStore.Load().Language;
        log.Info($"Language: {Strings.Language}.");

        var debugTools = new DebugTools(logHub, dataFolder, logFolder);
        var themeFixed = FindOption(args, "--theme=") is not null;
        if (Run(installer, ParseVersion(args), debugTools, debug, ParseSimulator(args), themeFixed, settingsStore))
        {
            // Started once the window is closed and the camera released, so the new instance can open it.
            log.Info("Restarting.");
            Process.Start(Application.ExecutablePath);
        }

        log.Info("Exited.");
    }

    /// <summary>Shows the main window until it closes; returns whether an update or a language change asked for a restart.</summary>
    /// <param name="simulator">Quirks of the "PTZ Simulator" camera to offer, or <c>null</c> for no simulator.</param>
    /// <param name="themeFixed">The theme was given on the command line: the saved theme does not replace it.</param>
    private static bool Run(UpdateInstaller installer, Version version, DebugTools debugTools, bool showDebugConsole, SimulatedCameraQuirks? simulator, bool themeFixed, IStore<AppSettings> settingsStore)
    {
        var dataFolder = debugTools.DataFolder;

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var form = new MainForm(debugTools) { ShowDebugConsoleOnStart = showDebugConsole, ThemeFixedByCommandLine = themeFixed };
        var tickers = new WinFormsTickerFactory();
        IVideoService videoService = new DirectShowVideoService(form.VideoHost);
        if (simulator is { } quirks)
        {
            videoService = new SimulatorVideoService(videoService, form.VideoHost, tickers, quirks, log.For("Simulator"));
        }

        var serialPorts = new SerialPortProvider();
        using var presenter = new MainPresenter(
            form,
            videoService,
            serialPorts,
            new CameraSessionFactory(videoService, serialPorts, tickers, log.For("Session")),
            settingsStore,
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

    /// <summary>Reads <c>--language=fr|en</c> (used by the CI tests); <c>null</c> keeps the saved language.</summary>
    private static AppLanguage? ParseLanguage(string[] args) =>
        FindOption(args, "--language=")?.ToLowerInvariant() switch
        {
            "en" or "english" => AppLanguage.English,
            "fr" or "french" => AppLanguage.French,
            _ => null,
        };

    /// <summary>
    /// Version used for the update check: the assembly's, unless <c>--simulate-version=x.y.z</c>
    /// is given (used by the smoke test to exercise the update with an older version).
    /// </summary>
    private static Version ParseVersion(string[] args)
    {
        var value = FindOption(args, "--simulate-version=");
        return value is not null && Version.TryParse(value, out var version) ? version : typeof(Program).Assembly.GetName().Version;
    }

    /// <summary>
    /// <c>--simulator</c> adds a simulated PTZ camera to the device list, to try the app without
    /// hardware; <c>--simulator=LaggingReadback,ReadbackIsLastTarget,IgnoresContinuousMoves</c>
    /// gives it the quirks of some real cameras.
    /// </summary>
    private static SimulatedCameraQuirks? ParseSimulator(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--simulator", StringComparison.OrdinalIgnoreCase)))
        {
            return SimulatedCameraQuirks.None;
        }

        var value = FindOption(args, "--simulator=");
        return value is null ? null
            : Enum.TryParse<SimulatedCameraQuirks>(value, ignoreCase: true, out var quirks) ? quirks
            : SimulatedCameraQuirks.None;
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
            exception.Message + Environment.NewLine + Environment.NewLine + Strings.DetailsInLog,
            Strings.UnexpectedErrorTitle,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
