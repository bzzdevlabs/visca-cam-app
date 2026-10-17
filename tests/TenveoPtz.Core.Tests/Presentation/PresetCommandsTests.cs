using System;
using System.IO;
using System.Linq;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Tests.Fakes;

namespace TenveoPtz.Core.Tests.Presentation;

/// <summary>Preset list commands (recall, update, rename, delete, reorder) and their failure paths.</summary>
public sealed class PresetCommandsTests : IDisposable
{
    private readonly FakeView view = new();
    private readonly FakeSessionFactory sessions = new();
    private readonly MemoryStore<AppSettings> settingsStore = new();
    private readonly MemoryStore<PresetBook> presetStore = new();
    private readonly MainPresenter presenter;

    public PresetCommandsTests()
    {
        var book = new PresetBook();
        book.Add("Wide");
        book.Add("Lectern");
        book.Add("Audience");
        presetStore.Save(book);
        var devices = new FakeDevices();
        presenter = new MainPresenter(view, devices, devices, sessions, settingsStore, presetStore, log: new Logger(new LogHub(LogLevel.Debug, 50), "Test"));
        view.Load();
    }

    private RecordingPtzController Ptz => sessions.Ptz;

    private Preset Wide => view.Presets[0];

    public void Dispose() => presenter.Dispose();

    [Fact]
    public void Recall_GoesToThePresetAndSaysSo()
    {
        view.Connect();

        view.RecallPreset(Wide);

        Assert.Contains($"Recall {Wide.Slot}", Ptz.Calls);
        Assert.Equal("Going to \"Wide\".", view.Status);
    }

    [Fact]
    public void Recall_WithoutConnectionReportsError()
    {
        view.RecallPreset(Wide);

        Assert.True(view.StatusIsError);
        Assert.Equal("Not connected. Press Connect first.", view.Status);
    }

    [Fact]
    public void RecallAt_AnEmptyNumberReportsError()
    {
        view.Connect();

        view.RecallPresetAt(8);

        Assert.True(view.StatusIsError);
        Assert.Equal("No preset #9.", view.Status);
        Assert.DoesNotContain(Ptz.Calls, c => c.StartsWith("Recall", StringComparison.Ordinal));
    }

    [Fact]
    public void Overwrite_StoresTheCurrentPositionWhenConfirmed()
    {
        view.Connect();

        view.OverwritePreset(Wide);

        Assert.Contains($"Store {Wide.Slot}", Ptz.Calls);
        Assert.Equal("Updated preset \"Wide\".", view.Status);
        Assert.Contains("Replace \"Wide\" with the current camera position?", view.Confirmations);
    }

    [Fact]
    public void Overwrite_DoesNothingWhenCancelled()
    {
        view.Connect();
        view.ConfirmAnswer = false;

        view.OverwritePreset(Wide);

        Assert.DoesNotContain(Ptz.Calls, c => c.StartsWith("Store", StringComparison.Ordinal));
    }

    [Fact]
    public void Overwrite_CameraFailureIsShownAndNothingIsSaved()
    {
        view.Connect();
        var saves = presetStore.SaveCount;
        Ptz.FailWith = new InvalidOperationException("camera busy");

        view.OverwritePreset(Wide);

        Assert.True(view.StatusIsError);
        Assert.Contains("camera busy", view.Status);
        Assert.Equal(saves, presetStore.SaveCount);
    }

    [Fact]
    public void Rename_SavesTheNewName()
    {
        view.PromptAnswer = "  Stage  ";

        view.RenamePreset(Wide);

        Assert.Equal("Stage", presetStore.Saved!.Items[0].Name);
        Assert.Equal("Renamed preset to \"Stage\".", view.Status);
    }

    [Fact]
    public void Rename_CancelledKeepsTheName()
    {
        view.PromptAnswer = null;
        var saves = presetStore.SaveCount;

        view.RenamePreset(Wide);

        Assert.Equal("Wide", presetStore.Saved!.Items[0].Name);
        Assert.Equal(saves, presetStore.SaveCount);
    }

    [Fact]
    public void Rename_ToAnEmptyNameIsRefused()
    {
        view.PromptAnswer = "   ";

        view.RenamePreset(Wide);

        Assert.True(view.StatusIsError);
        Assert.StartsWith("A preset name is required.", view.Status);
        Assert.Equal("Wide", Wide.Name);
    }

    [Fact]
    public void Delete_WithoutConnectionOnlyRemovesItFromTheList()
    {
        view.DeletePreset(Wide);

        Assert.Equal(new[] { "Lectern", "Audience" }, presetStore.Saved!.Items.Select(p => p.Name));
        Assert.Empty(Ptz.Calls);
        Assert.Equal("Deleted preset \"Wide\".", view.Status);
    }

    [Fact]
    public void Move_ReordersAndPersistsWithoutAStatus()
    {
        view.ShowStatus("before", false);
        var audience = view.Presets[2];

        view.MovePreset(audience, -2);

        Assert.Equal(new[] { "Audience", "Wide", "Lectern" }, presetStore.Saved!.Items.Select(p => p.Name));
        Assert.Equal("Audience", view.Presets[0].Name);
        Assert.Equal("before", view.Status);
    }

    [Fact]
    public void Add_CancelledPromptAddsNothing()
    {
        view.Connect();
        view.PromptAnswer = null;

        view.AddPreset();

        Assert.Equal(3, presetStore.Saved!.Items.Count);
        Assert.DoesNotContain(Ptz.Calls, c => c.StartsWith("Store", StringComparison.Ordinal));
    }

    [Fact]
    public void Add_OffersTheNextNumberAsName()
    {
        view.Connect();
        view.PromptAnswer = "Preset 4";

        view.AddPreset();

        Assert.Equal("Saved preset \"Preset 4\".", view.Status);
        Assert.Equal(4, presetStore.Saved!.Items.Count);
    }

    [Fact]
    public void Add_WhenEverySlotIsTakenReportsError()
    {
        var full = new PresetBook();
        for (var i = PresetBook.FirstSlot; i <= PresetBook.LastSlot; i++)
        {
            full.Add("P" + i);
        }

        presetStore.Save(full);
        view.Load();
        view.Connect();
        view.PromptAnswer = "One more";

        view.AddPreset();

        Assert.True(view.StatusIsError);
        Assert.Contains("preset slots are in use", view.Status);
    }

    [Fact]
    public void SaveFailure_IsShownAsError()
    {
        presetStore.FailWith = new IOException("disk full");
        view.PromptAnswer = "Stage";

        view.RenamePreset(Wide);

        Assert.True(view.StatusIsError);
        Assert.Equal("Could not save presets: disk full", view.Status);
    }

    [Theory]
    [InlineData(typeof(UnauthorizedAccessException))]
    [InlineData(typeof(InvalidOperationException))]
    public void SettingsSaveFailure_IsShownAsError(Type failure)
    {
        settingsStore.FailWith = (Exception)Activator.CreateInstance(failure, "read-only")!;

        view.Close();

        Assert.True(view.StatusIsError);
        Assert.Equal("Could not save settings: read-only", view.Status);
    }
}
