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

    /// <summary>Reverses pan, e.g. for a ceiling-mounted camera.</summary>
    public bool InvertPan { get; set; }

    /// <summary>Reverses tilt, e.g. for a ceiling-mounted camera.</summary>
    public bool InvertTilt { get; set; }
}
