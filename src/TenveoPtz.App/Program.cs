using System;
using System.IO;
using System.Windows.Forms;
using TenveoPtz.App.Infrastructure.DirectShow;
using TenveoPtz.App.Infrastructure.Serial;
using TenveoPtz.App.Infrastructure.Timing;
using TenveoPtz.App.Views;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Session;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Storage;

namespace TenveoPtz.App;

/// <summary>Composition root: wires the concrete Windows services into the presenter.</summary>
internal static class Program
{
    private const string DataFolderName = "TenveoPTZ";

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.ThreadException += (_, e) => ShowFatal(e.Exception);

        var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DataFolderName);

        using var form = new MainForm();
        var videoService = new DirectShowVideoService(form.VideoHost);
        var serialPorts = new SerialPortProvider();
        using var presenter = new MainPresenter(
            form,
            videoService,
            serialPorts,
            new CameraSessionFactory(videoService, serialPorts, new WinFormsTickerFactory()),
            new XmlFileStore<AppSettings>(Path.Combine(dataFolder, "settings.xml")),
            new XmlFileStore<PresetBook>(Path.Combine(dataFolder, "presets.xml")));

        Application.Run(form);
    }

    private static void ShowFatal(Exception exception) =>
        MessageBox.Show(exception.Message, "Tenveo PTZ - unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
