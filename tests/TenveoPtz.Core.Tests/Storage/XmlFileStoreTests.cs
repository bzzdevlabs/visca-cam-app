using System;
using System.IO;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Storage;

namespace TenveoPtz.Core.Tests.Storage;

public sealed class XmlFileStoreTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "TenveoPtzTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenFileIsMissing()
    {
        var settings = new XmlFileStore<AppSettings>(Path.Combine(folder, "settings.xml")).Load();

        Assert.Equal(ConnectionOptions.DefaultBaudRate, settings.Connection.BaudRate);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsPresets()
    {
        var store = new XmlFileStore<PresetBook>(Path.Combine(folder, "presets.xml"));
        var book = new PresetBook();
        book.Add("Pulpit").Position = new PtzPosition(10, -5, null);
        book.Add("Choir");

        store.Save(book);
        store.Save(book); // Second save exercises the atomic replace path.
        var loaded = store.Load();

        Assert.Equal(2, loaded.Items.Count);
        Assert.Equal("Pulpit", loaded.Items[0].Name);
        Assert.Equal(10, loaded.Items[0].Position?.Pan);
        Assert.Null(loaded.Items[0].Position?.Zoom);
        Assert.Null(loaded.Items[1].Position);
        Assert.Equal(2, loaded.Items[1].Slot);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsSettings()
    {
        var store = new XmlFileStore<AppSettings>(Path.Combine(folder, "settings.xml"));
        store.Save(new AppSettings
        {
            Connection = new ConnectionOptions { Mode = ControlMode.Visca, PortName = "COM4", Address = 2 },
            InvertTilt = true,
        });

        var loaded = store.Load();

        Assert.Equal(ControlMode.Visca, loaded.Connection.Mode);
        Assert.Equal("COM4", loaded.Connection.PortName);
        Assert.Equal(2, loaded.Connection.Address);
        Assert.True(loaded.InvertTilt);
    }

    [Fact]
    public void Load_KeepsCorruptFileAside()
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "presets.xml");
        File.WriteAllText(path, "<not xml");

        var loaded = new XmlFileStore<PresetBook>(path).Load();

        Assert.Empty(loaded.Items);
        Assert.True(File.Exists(path + ".corrupt"));
    }
}
