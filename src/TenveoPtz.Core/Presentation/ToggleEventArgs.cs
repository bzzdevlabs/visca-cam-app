using System;

namespace TenveoPtz.Core.Presentation;

public sealed class ToggleEventArgs : EventArgs
{
    public ToggleEventArgs(bool enabled)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
}
