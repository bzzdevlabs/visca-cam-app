using System;

namespace TenveoPtz.Core.Transport;

/// <summary>Bidirectional byte stream to a device (typically a serial port).</summary>
public interface IByteTransport : IDisposable
{
    void Write(byte[] data);

    /// <summary>Reads one byte, or returns -1 when nothing arrives within <paramref name="timeout"/>.</summary>
    int ReadByte(TimeSpan timeout);

    /// <summary>Drops any unread input, e.g. stale replies from earlier commands.</summary>
    void DiscardInput();
}
