using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using TenveoPtz.App.Infrastructure.DirectShow;
using TenveoPtz.App.Infrastructure.Serial;
using TenveoPtz.App.Infrastructure.Timing;
using TenveoPtz.App.Views;
using TenveoPtz.App.Views.Theming;
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

    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.ThreadException += (_, e) => ShowFatal(e.Exception);

        Theme.Initialize(ParseTheme(args));
        var installer = new UpdateInstaller(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(Application.ExecutablePath));
        installer.DeleteLeftovers();

        if (Run(installer, ParseVersion(args)))
        {
            // Started once the window is closed and the camera released, so the new instance can open it.
            Process.Start(Application.ExecutablePath);
        }
    }

    /// <summary>Shows the main window until it closes; returns whether an update asked for a restart.</summary>
    private static bool Run(UpdateInstaller installer, Version version)
    {
        var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DataFolderName);

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var form = new MainForm();
        var videoService = new DirectShowVideoService(form.VideoHost);
        var serialPorts = new SerialPortProvider();
        using var presenter = new MainPresenter(
            form,
            videoService,
            serialPorts,
            new CameraSessionFactory(videoService, serialPorts, new WinFormsTickerFactory()),
            new XmlFileStore<AppSettings>(Path.Combine(dataFolder, "settings.xml")),
            new XmlFileStore<PresetBook>(Path.Combine(dataFolder, "presets.xml")),
            new GitHubUpdateService(http, Repository, version, installer),
            version);

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

    private static void ShowFatal(Exception exception) =>
        MessageBox.Show(exception.Message, "Tenveo PTZ - unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
