using System;
using TenveoPtz.Core.Presets;

namespace TenveoPtz.Core.Presentation;

public sealed class PresetMoveEventArgs : EventArgs
{
    public PresetMoveEventArgs(Preset preset, int offset)
    {
        Preset = preset ?? throw new ArgumentNullException(nameof(preset));
        Offset = offset;
    }

    public Preset Preset { get; }

    /// <summary>Positions to move by: negative moves up the list.</summary>
    public int Offset { get; }
}
