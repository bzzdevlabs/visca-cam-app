using System;

namespace TenveoPtz.Core.Presentation;

/// <summary>Refers to a preset by its zero-based position in the list (hotkeys 1..9).</summary>
public sealed class PresetIndexEventArgs : EventArgs
{
    public PresetIndexEventArgs(int index)
    {
        Index = index;
    }

    public int Index { get; }
}
