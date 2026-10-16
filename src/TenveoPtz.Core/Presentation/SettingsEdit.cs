using System;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.Core.Presentation;

/// <summary>Outcome of the settings panel: the edited settings, or a request to reset them all.</summary>
public sealed class SettingsEdit
{
    private SettingsEdit(AppSettings? settings, bool resetRequested)
    {
        Settings = settings;
        ResetRequested = resetRequested;
    }

    /// <summary>The settings to apply; <c>null</c> when <see cref="ResetRequested"/>.</summary>
    public AppSettings? Settings { get; }

    /// <summary>The user asked to restore every setting to its default.</summary>
    public bool ResetRequested { get; }

    public static SettingsEdit Save(AppSettings settings) => new(settings ?? throw new ArgumentNullException(nameof(settings)), false);

    public static SettingsEdit Reset() => new(null, true);
}
