using System;
using System.IO;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Presets;

namespace TenveoPtz.Core.Presentation;

/// <summary>Preset management: create, recall, overwrite, rename, delete and reorder.</summary>
public sealed partial class MainPresenter
{
    private void SubscribePresetEvents()
    {
        view.AddPresetRequested += (_, _) => AddPreset();
        view.RecallPresetRequested += (_, e) => RecallPreset(e.Preset);
        view.RecallPresetAtRequested += (_, e) => RecallPresetAt(e.Index);
        view.OverwritePresetRequested += (_, e) => OverwritePreset(e.Preset);
        view.RenamePresetRequested += (_, e) => RenamePreset(e.Preset);
        view.DeletePresetRequested += (_, e) => DeletePreset(e.Preset);
        view.MovePresetRequested += (_, e) => MovePreset(e.Preset, e.Offset);
    }

    private void AddPreset()
    {
        if (session is null)
        {
            ShowStatus(Strings.ConnectBeforeSavingPreset, true);
            return;
        }

        var name = view.PromptText(Strings.NewPresetTitle, Strings.NewPresetPrompt, Strings.DefaultPresetName(presets.Items.Count + 1));
        if (name is null)
        {
            return;
        }

        Preset preset;
        try
        {
            preset = presets.Add(name);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ShowStatus(ex.Message, true);
            return;
        }

        if (!WithCamera(ptz => ptz.StorePreset(preset)))
        {
            presets.Remove(preset);
            return;
        }

        SavePresets(preset, Strings.SavedPreset(preset.Name));
    }

    private void RecallPreset(Preset preset)
    {
        if (WithCamera(ptz => ptz.RecallPreset(preset)))
        {
            view.ShowPresets(presets.Items, preset);
            ShowStatus(Strings.GoingToPreset(preset.Name), false);
        }
    }

    private void RecallPresetAt(int index)
    {
        var preset = presets.At(index);
        if (preset is null)
        {
            ShowStatus(Strings.NoPresetNumber(index + 1), true);
            return;
        }

        RecallPreset(preset);
    }

    private void OverwritePreset(Preset preset)
    {
        if (!view.Confirm(Strings.ReplacePreset(preset.Name)))
        {
            return;
        }

        if (WithCamera(ptz => ptz.StorePreset(preset)))
        {
            SavePresets(preset, Strings.UpdatedPreset(preset.Name));
        }
    }

    private void RenamePreset(Preset preset)
    {
        var name = view.PromptText(Strings.RenamePresetTitle, Strings.RenamePresetPrompt, preset.Name);
        if (name is null)
        {
            return;
        }

        try
        {
            presets.Rename(preset, name);
        }
        catch (ArgumentException ex)
        {
            ShowStatus(ex.Message, true);
            return;
        }

        SavePresets(preset, Strings.RenamedPreset(preset.Name));
    }

    private void DeletePreset(Preset preset)
    {
        if (!view.Confirm(Strings.DeletePreset(preset.Name)))
        {
            return;
        }

        if (session is not null)
        {
            WithCamera(ptz => ptz.ForgetPreset(preset));
        }

        presets.Remove(preset);
        SavePresets(null, Strings.DeletedPreset(preset.Name));
    }

    private void MovePreset(Preset preset, int offset)
    {
        presets.Move(preset, offset);
        SavePresets(preset, null);
    }

    private void SavePresets(Preset? selected, string? status)
    {
        view.ShowPresets(presets.Items, selected);
        try
        {
            presetStore.Save(presets);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
        {
            ShowStatus(Strings.CouldNotSavePresets(ex.Message), true, ex);
            return;
        }

        if (status is not null)
        {
            ShowStatus(status, false);
        }
    }
}
