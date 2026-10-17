using TenveoPtz.Core.Localization;
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

        if (settings.Language != previous.Language)
        {
            OfferRestartForLanguage();
            return;
        }

        // The UVC move mode is chosen when the camera opens.
        if (IsConnected && previous.UvcContinuousMoves != settings.UvcContinuousMoves && settings.Connection.Mode == ControlMode.Uvc)
        {
            Connect();
        }
        else
        {
            ShowStatus(Strings.SettingsSaved, false);
        }
    }

    private void ResetSettings()
    {
        if (!view.Confirm(Strings.ConfirmReset))
        {
            return;
        }

        var previousLanguage = settings.Language;
        CloseSession();
        settings = new AppSettings();
        ApplyToView(includeConnection: true);
        view.ShowConnected(false);
        TrySave(() => settingsStore.Save(settings));
        log.Info("Settings reset to their defaults.");
        ShowStatus(Strings.ResetDone, false);
        if (settings.Language != previousLanguage)
        {
            OfferRestartForLanguage();
        }
    }

    /// <summary>The windows are built in the language read at start-up: a new one needs a restart.</summary>
    private void OfferRestartForLanguage()
    {
        log.Info($"Language changed to {settings.Language}.");
        if (view.Confirm(Strings.RestartForLanguage(settings.Language)))
        {
            log.Info("Restarting to change the language.");
            view.Restart();
        }
        else
        {
            ShowStatus(Strings.LanguageAtNextStart, false);
        }
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
