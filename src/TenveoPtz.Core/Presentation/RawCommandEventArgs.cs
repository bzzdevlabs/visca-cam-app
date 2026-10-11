using System;

namespace TenveoPtz.Core.Presentation;

/// <summary>Protocol bytes as typed by the user, e.g. "81 01 06 04 FF".</summary>
public sealed class RawCommandEventArgs : EventArgs
{
    public RawCommandEventArgs(string text)
    {
        Text = text ?? string.Empty;
    }

    public string Text { get; }
}
