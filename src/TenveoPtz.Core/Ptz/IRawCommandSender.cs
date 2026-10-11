using System.Collections.Generic;

namespace TenveoPtz.Core.Ptz;

/// <summary>A PTZ strategy that can send protocol bytes typed by hand (debug console).</summary>
public interface IRawCommandSender
{
    /// <summary>Queues <paramref name="bytes"/> for sending; the camera's reply is logged.</summary>
    /// <exception cref="System.ArgumentException">The bytes do not form a valid command.</exception>
    void SendRaw(IReadOnlyList<byte> bytes);
}
