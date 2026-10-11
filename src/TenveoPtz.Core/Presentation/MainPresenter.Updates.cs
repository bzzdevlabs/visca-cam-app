using System;
using TenveoPtz.Core.Updates;

namespace TenveoPtz.Core.Presentation;

/// <summary>Start-up check for a newer release, installed when the user agrees.</summary>
public sealed partial class MainPresenter
{
    private bool closing;

    /// <summary>
    /// Looks for a newer release in the background. Being offline or rate-limited is not worth
    /// bothering the user about, so a failed check is silent.
    /// </summary>
    private async void CheckForUpdates()
    {
        if (updates is null || !settings.CheckForUpdates)
        {
            return;
        }

        ReleaseInfo? release;
        try
        {
            release = await updates.FindUpdateAsync();
        }
        catch (Exception ex)
        {
            log.Info("Update check failed: " + ex.Message);
            return;
        }

        log.Info(release is null ? "No newer release." : $"Version {release.Version} is available.");
        if (release is null || closing
            || !view.ConfirmUpdate($"Version {release.Version} of Tenveo PTZ is available (you have {currentVersion}). Download and install it now? The app restarts when it is done."))
        {
            return;
        }

        ShowStatus($"Downloading version {release.Version}...", false);
        try
        {
            await updates.InstallAsync(release);
        }
        catch (Exception ex)
        {
            ShowStatus($"Update failed: {ex.Message} You can download it from {release.PageUrl}", true, ex);
            return;
        }

        if (!closing)
        {
            view.Restart();
        }
    }
}
