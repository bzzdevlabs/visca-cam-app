using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Presets;

/// <summary>A named camera position.</summary>
public sealed class Preset
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Camera memory slot used in VISCA mode.</summary>
    public int Slot { get; set; }

    /// <summary>Absolute position captured in UVC mode, if any.</summary>
    public PtzPosition? Position { get; set; }

    public override string ToString() => Name;
}
