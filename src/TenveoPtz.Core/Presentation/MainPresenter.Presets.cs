using System;
using System.IO;
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
            view.ShowStatus("Connect to the camera before saving a preset.", true);
            return;
        }

        var name = view.PromptText("New preset", "Name of the current camera position:", $"Preset {presets.Items.Count + 1}");
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
            view.ShowStatus(ex.Message, true);
            return;
        }

        if (!WithCamera(ptz => ptz.StorePreset(preset)))
        {
            presets.Remove(preset);
            return;
        }

        SavePresets(preset, $"Saved preset \"{preset.Name}\".");
    }

    private void RecallPreset(Preset preset)
    {
        if (WithCamera(ptz => ptz.RecallPreset(preset)))
        {
            view.ShowPresets(presets.Items, preset);
            view.ShowStatus($"Going to \"{preset.Name}\".", false);
        }
    }

    private void RecallPresetAt(int index)
    {
        var preset = presets.At(index);
        if (preset is null)
        {
            view.ShowStatus($"No preset #{index + 1}.", true);
            return;
        }

        RecallPreset(preset);
    }

    private void OverwritePreset(Preset preset)
    {
        if (!view.Confirm($"Replace \"{preset.Name}\" with the current camera position?"))
        {
            return;
        }

        if (WithCamera(ptz => ptz.StorePreset(preset)))
        {
            SavePresets(preset, $"Updated preset \"{preset.Name}\".");
        }
    }

    private void RenamePreset(Preset preset)
    {
        var name = view.PromptText("Rename preset", "New name:", preset.Name);
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
            view.ShowStatus(ex.Message, true);
            return;
        }

        SavePresets(preset, $"Renamed preset to \"{preset.Name}\".");
    }

    private void DeletePreset(Preset preset)
    {
        if (!view.Confirm($"Delete preset \"{preset.Name}\"?"))
        {
            return;
        }

        if (session is not null)
        {
            WithCamera(ptz => ptz.ForgetPreset(preset));
        }

        presets.Remove(preset);
        SavePresets(null, $"Deleted preset \"{preset.Name}\".");
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
            view.ShowStatus("Could not save presets: " + ex.Message, true);
            return;
        }

        if (status is not null)
        {
            view.ShowStatus(status, false);
        }
    }
}
