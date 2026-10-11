using System;
using System.IO;
using System.IO.Compression;
using TenveoPtz.Core.Updates;

namespace TenveoPtz.Core.Tests.Updates;

public sealed class UpdateInstallerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "TenveoPtzTests-" + Guid.NewGuid().ToString("N"));
    private readonly string app;
    private readonly UpdateInstaller installer;

    public UpdateInstallerTests()
    {
        app = Directory.CreateDirectory(Path.Combine(root, "app")).FullName;
        installer = new UpdateInstaller(app, "TenveoPTZ.exe");
    }

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Install_ReplacesFilesAndKeepsOldOnesAside()
    {
        File.WriteAllText(Path.Combine(app, "TenveoPTZ.exe"), "old exe");
        File.WriteAllText(Path.Combine(app, "TenveoPtz.Core.dll"), "old dll");
        var archive = CreateArchive(("TenveoPTZ.exe", "new exe"), ("TenveoPtz.Core.dll", "new dll"), ("README.md", "readme"));

        installer.Install(archive);

        Assert.Equal("new exe", File.ReadAllText(Path.Combine(app, "TenveoPTZ.exe")));
        Assert.Equal("new dll", File.ReadAllText(Path.Combine(app, "TenveoPtz.Core.dll")));
        Assert.Equal("readme", File.ReadAllText(Path.Combine(app, "README.md")));
        Assert.Equal("old exe", File.ReadAllText(Path.Combine(app, "TenveoPTZ.exe.update-old")));
    }

    [Fact]
    public void Install_RejectsArchiveWithoutExecutable()
    {
        File.WriteAllText(Path.Combine(app, "TenveoPTZ.exe"), "old exe");
        var archive = CreateArchive(("something.txt", "not the app"));

        Assert.Throws<InvalidDataException>(() => installer.Install(archive));

        Assert.Equal("old exe", File.ReadAllText(Path.Combine(app, "TenveoPTZ.exe")));
        Assert.False(File.Exists(Path.Combine(app, "something.txt")));
    }

    [Fact]
    public void Install_FailureRestoresOriginalFiles()
    {
        File.WriteAllText(Path.Combine(app, "TenveoPTZ.exe"), "old exe");
        Directory.CreateDirectory(Path.Combine(app, "zz-blocked")); // A folder where a file must go: copying fails.
        var archive = CreateArchive(("TenveoPTZ.exe", "new exe"), ("zz-blocked", "file"));

        Assert.ThrowsAny<Exception>(() => installer.Install(archive));

        Assert.Equal("old exe", File.ReadAllText(Path.Combine(app, "TenveoPTZ.exe")));
        Assert.False(File.Exists(Path.Combine(app, "TenveoPTZ.exe.update-old")));
    }

    [Fact]
    public void DeleteLeftovers_RemovesOnlyUpdateBackups()
    {
        File.WriteAllText(Path.Combine(app, "TenveoPTZ.exe.update-old"), "previous");
        File.WriteAllText(Path.Combine(app, "notes.old"), "user file");

        installer.DeleteLeftovers();

        Assert.False(File.Exists(Path.Combine(app, "TenveoPTZ.exe.update-old")));
        Assert.True(File.Exists(Path.Combine(app, "notes.old")));
    }

    private string CreateArchive(params (string Name, string Content)[] entries)
    {
        var path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".zip");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            using var writer = new StreamWriter(zip.CreateEntry(name).Open());
            writer.Write(content);
        }

        return path;
    }
}
