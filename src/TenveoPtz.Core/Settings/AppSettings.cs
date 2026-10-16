namespace TenveoPtz.Core.Settings;

/// <summary>User preferences persisted between runs.</summary>
public sealed class AppSettings
{
    public ConnectionOptions Connection { get; set; } = new();

    /// <summary>Reconnect automatically on start-up (the app was connected when last closed).</summary>
    public bool AutoConnect { get; set; }

    /// <summary>Relative movement speed, 0..1.</summary>
    public double Speed { get; set; } = 0.5;

    public bool AlwaysOnTop { get; set; }

    /// <summary>The numeric keypad controls the camera even while another application is in front (Windows).</summary>
    public bool BackgroundNumpad { get; set; }

    /// <summary>
    /// UVC: move with the camera's continuous (relative) controls instead of absolute steps.
    /// Off by default: some cameras report these controls without honouring them.
    /// </summary>
    public bool UvcContinuousMoves { get; set; }

    /// <summary>Reverses pan, e.g. for a ceiling-mounted camera.</summary>
    public bool InvertPan { get; set; }

    /// <summary>Reverses tilt, e.g. for a ceiling-mounted camera.</summary>
    public bool InvertTilt { get; set; }

    /// <summary>Looks for a newer release on start-up and offers to install it.</summary>
    public bool CheckForUpdates { get; set; } = true;

    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>Language of the user interface; applies when the app starts.</summary>
    public AppLanguage Language { get; set; } = AppLanguage.French;

    /// <summary>Independent copy, for the settings panel to edit.</summary>
    public AppSettings Clone()
    {
        var copy = (AppSettings)MemberwiseClone();
        copy.Connection = Connection.Clone();
        return copy;
    }
}
