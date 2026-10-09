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

    public ConnectionOptions Clone() => (ConnectionOptions)MemberwiseClone();
}
