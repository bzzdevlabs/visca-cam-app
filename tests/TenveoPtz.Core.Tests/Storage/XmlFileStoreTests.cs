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

    [Fact]
    public void Load_UnreadableFileStartsWithDefaults()
    {
        // A folder where the file should be cannot be read, like a locked file.
        var path = Directory.CreateDirectory(Path.Combine(folder, "settings.xml")).FullName;

        var loaded = new XmlFileStore<AppSettings>(path).Load();

        Assert.Equal(0.5, loaded.Speed);
    }

    [Fact]
    public void Load_CorruptFileStillLoadsWhenItCannotBeKeptAside()
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "presets.xml");
        File.WriteAllText(path, "<not xml");
        Directory.CreateDirectory(path + ".corrupt");

        var loaded = new XmlFileStore<PresetBook>(path).Load();

        Assert.Empty(loaded.Items);
    }

    [Fact]
    public void Save_CreatesTheFolder()
    {
        var path = Path.Combine(folder, "nested", "settings.xml");

        new XmlFileStore<AppSettings>(path).Save(new AppSettings { Speed = 0.2 });

        Assert.Equal(0.2, new XmlFileStore<AppSettings>(path).Load().Speed);
    }

    [Fact]
    public void Language_DefaultsToFrenchInOlderFiles()
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "settings.xml");
        File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?><AppSettings><Speed>0.3</Speed></AppSettings>");

        var loaded = new XmlFileStore<AppSettings>(path).Load();

        Assert.Equal(AppLanguage.French, loaded.Language);
        Assert.Equal(0.3, loaded.Speed);
    }

    [Fact]
    public void Guards_RejectMissingArguments()
    {
        Assert.Throws<ArgumentException>(() => new XmlFileStore<AppSettings>(" "));
        Assert.Throws<ArgumentNullException>(() => new XmlFileStore<AppSettings>(Path.Combine(folder, "s.xml")).Save(null!));
    }
}
