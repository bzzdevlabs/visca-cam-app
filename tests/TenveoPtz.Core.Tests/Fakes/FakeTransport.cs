using System;
using System.Collections.Generic;
using TenveoPtz.Core.Transport;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>In-memory transport: records writes and plays back scripted camera replies.</summary>
internal sealed class FakeTransport : IByteTransport
{
    private readonly Queue<byte> replies = new();

    public List<byte[]> Written { get; } = new();

    public bool Disposed { get; private set; }

    public void QueueReply(params byte[] bytes)
    {
        foreach (var b in bytes)
        {
            replies.Enqueue(b);
        }
    }

    public void Write(byte[] data) => Written.Add(data);

    public int ReadByte(TimeSpan timeout) => replies.Count > 0 ? replies.Dequeue() : -1;

    public void DiscardInput()
    {
        // Replies are queued for the next command on purpose, so nothing is discarded here.
    }

    public void Dispose() => Disposed = true;
}
