using System;
using System.IO;
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
    public void Install_ReplacesExecutableAndKeepsOldOneAside()
    {
        File.WriteAllText(Path.Combine(app, "TenveoPTZ.exe"), "MZ old");

        installer.Install(Download("MZ new"));

        Assert.Equal("MZ new", File.ReadAllText(Path.Combine(app, "TenveoPTZ.exe")));
        Assert.Equal("MZ old", File.ReadAllText(Path.Combine(app, "TenveoPTZ.exe.update-old")));
    }

    [Fact]
    public void Install_ReplacesPreviousBackup()
    {
        File.WriteAllText(Path.Combine(app, "TenveoPTZ.exe"), "MZ old");
        File.WriteAllText(Path.Combine(app, "TenveoPTZ.exe.update-old"), "MZ older");

        installer.Install(Download("MZ new"));

        Assert.Equal("MZ old", File.ReadAllText(Path.Combine(app, "TenveoPTZ.exe.update-old")));
    }

    [Fact]
    public void Install_RejectsFileThatIsNotAnExecutable()
    {
        File.WriteAllText(Path.Combine(app, "TenveoPTZ.exe"), "MZ old");

        Assert.Throws<InvalidDataException>(() => installer.Install(Download("<html>rate limited</html>")));

        Assert.Equal("MZ old", File.ReadAllText(Path.Combine(app, "TenveoPTZ.exe")));
        Assert.False(File.Exists(Path.Combine(app, "TenveoPTZ.exe.update-old")));
    }

    [Fact]
    public void Install_FailureRestoresOriginalExecutable()
    {
        var target = Path.Combine(app, "TenveoPTZ.exe");
        File.WriteAllText(target, "MZ old");

        // The package is the executable itself: once it is moved aside, the copy finds nothing.
        Assert.ThrowsAny<IOException>(() => installer.Install(target));

        Assert.Equal("MZ old", File.ReadAllText(target));
        Assert.False(File.Exists(target + ".update-old"));
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

    private string Download(string content)
    {
        var path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".exe");
        File.WriteAllText(path, content);
        return path;
    }
}
