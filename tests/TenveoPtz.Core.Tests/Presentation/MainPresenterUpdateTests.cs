using System;
using System.Net.Http;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Tests.Fakes;

namespace TenveoPtz.Core.Tests.Presentation;

public sealed class MainPresenterUpdateTests : IDisposable
{
    private readonly FakeView view = new();
    private readonly FakeUpdateService updates = new();
    private readonly MemoryStore<AppSettings> settingsStore = new();
    private readonly MainPresenter presenter;

    public MainPresenterUpdateTests()
    {
        var devices = new FakeDevices();
        presenter = new MainPresenter(view, devices, devices, new FakeSessionFactory(), settingsStore, new MemoryStore<PresetBook>(), updates, new Version(1, 0, 0, 5));
    }

    public void Dispose() => presenter.Dispose();

    [Fact]
    public void Load_AcceptedUpdateIsInstalledThenAppRestarts()
    {
        updates.Available = FakeUpdateService.Release("1.1.0.9");

        view.Load();

        Assert.Contains("1.1.0.9", view.UpdateQuestion);
        Assert.Contains("1.0.0.5", view.UpdateQuestion);
        Assert.Same(updates.Available, updates.Installed);
        Assert.True(view.Restarted);
    }

    [Fact]
    public void Load_DeclinedUpdateIsNotInstalled()
    {
        updates.Available = FakeUpdateService.Release("1.1.0.9");
        view.UpdateAnswer = false;

        view.Load();

        Assert.NotNull(view.UpdateQuestion);
        Assert.Null(updates.Installed);
        Assert.False(view.Restarted);
    }

    [Fact]
    public void Load_NoUpdateMeansNoQuestion()
    {
        view.Load();

        Assert.Equal(1, updates.CheckCount);
        Assert.Null(view.UpdateQuestion);
    }

    [Fact]
    public void Load_FailedCheckIsSilent()
    {
        updates.CheckFailure = new HttpRequestException("offline");

        view.Load();

        Assert.Null(view.UpdateQuestion);
        Assert.False(view.StatusIsError);
    }

    [Fact]
    public void Load_CheckCanBeTurnedOffInSettings()
    {
        settingsStore.Save(new AppSettings { CheckForUpdates = false });
        updates.Available = FakeUpdateService.Release("1.1.0.9");

        view.Load();

        Assert.Equal(0, updates.CheckCount);
        Assert.Null(view.UpdateQuestion);
    }

    [Fact]
    public void Install_FailureIsShownWithReleasePageAndAppKeepsRunning()
    {
        updates.Available = FakeUpdateService.Release("1.1.0.9");
        updates.InstallFailure = new UnauthorizedAccessException("Access denied.");

        view.Load();

        Assert.True(view.StatusIsError);
        Assert.Contains("Access denied.", view.Status);
        Assert.Contains("https://example.test/releases/v1.1.0.9", view.Status);
        Assert.False(view.Restarted);
    }
}
