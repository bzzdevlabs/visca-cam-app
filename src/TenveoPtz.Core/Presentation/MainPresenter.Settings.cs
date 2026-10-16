using TenveoPtz.Core.Settings;

namespace TenveoPtz.Core.Presentation;

/// <summary>The settings panel: edit, apply and save the settings, or reset them all.</summary>
public sealed partial class MainPresenter
{
    private void EditSettings()
    {
        ReadViewState();
        var edit = view.EditSettings(settings.Clone());
        if (edit is null)
        {
            return;
        }

        if (edit.ResetRequested)
        {
            ResetSettings();
            return;
        }

        var previous = settings;
        settings = edit.Settings!;
        ApplyToView(includeConnection: false);
        TrySave(() => settingsStore.Save(settings));
        log.Info("Settings changed.");

        // The UVC move mode is chosen when the camera opens.
        if (IsConnected && previous.UvcContinuousMoves != settings.UvcContinuousMoves && settings.Connection.Mode == ControlMode.Uvc)
        {
            Connect();
        }
        else
        {
            ShowStatus("Settings saved.", false);
        }
    }

    private void ResetSettings()
    {
        if (!view.Confirm("Restore every setting to its default? This disconnects the camera and resets the camera choice, speed, keys, theme and other options. Presets are kept."))
        {
            return;
        }

        CloseSession();
        settings = new AppSettings();
        ApplyToView(includeConnection: true);
        view.ShowConnected(false);
        TrySave(() => settingsStore.Save(settings));
        log.Info("Settings reset to their defaults.");
        ShowStatus("All settings were reset to their defaults. Presets were kept.", false);
    }

    /// <summary>Copies what the window holds (camera choice, speed, toggles) into the settings.</summary>
    private void ReadViewState()
    {
        settings.Connection = view.ConnectionOptions.Clone();
        settings.Speed = view.Speed;
        settings.AlwaysOnTop = view.AlwaysOnTop;
        settings.BackgroundNumpad = view.BackgroundNumpad;
    }

    private void ApplyToView(bool includeConnection)
    {
        if (includeConnection)
        {
            view.ConnectionOptions = settings.Connection.Clone();
            view.Speed = settings.Speed;
        }

        view.AlwaysOnTop = settings.AlwaysOnTop;
        view.BackgroundNumpad = settings.BackgroundNumpad;
        view.Theme = settings.Theme;
    }
}
