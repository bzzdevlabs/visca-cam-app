using System;
using TenveoPtz.Core.Presets;

namespace TenveoPtz.Core.Presentation;

public sealed class PresetEventArgs : EventArgs
{
    public PresetEventArgs(Preset preset)
    {
        Preset = preset ?? throw new ArgumentNullException(nameof(preset));
    }

    public Preset Preset { get; }
}
