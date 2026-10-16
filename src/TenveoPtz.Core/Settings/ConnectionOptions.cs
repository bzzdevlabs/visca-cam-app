namespace TenveoPtz.Core.Settings;

/// <summary>Everything needed to open a camera session.</summary>
public sealed class ConnectionOptions
{
    public const int DefaultBaudRate = 9600;

    public string VideoDeviceName { get; set; } = string.Empty;

    public ControlMode Mode { get; set; } = ControlMode.Uvc;

    public string PortName { get; set; } = string.Empty;

    public int BaudRate { get; set; } = DefaultBaudRate;

    /// <summary>VISCA camera address (1..7).</summary>
    public int Address { get; set; } = 1;

    /// <summary>
    /// Shows the live video. When off, the camera's video stays free for other applications
    /// (Zoom, Teams...): on Windows only one application at a time can receive it.
    /// </summary>
    public bool ShowPreview { get; set; } = true;

    /// <summary>Copied from <see cref="AppSettings.UvcContinuousMoves"/> when connecting; not saved here.</summary>
    [System.Xml.Serialization.XmlIgnore]
    public bool UvcContinuousMoves { get; set; }

    public ConnectionOptions Clone() => (ConnectionOptions)MemberwiseClone();
}
