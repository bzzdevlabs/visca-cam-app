using System;
using System.Collections.Generic;

namespace TenveoPtz.Core.Visca;

/// <summary>
/// Immutable VISCA command (Command pattern): a named payload that can be framed for any camera address.
/// </summary>
public sealed class ViscaCommand
{
    public const byte Terminator = 0xFF;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(500);

    private readonly byte[] payload;

    /// <param name="waitForCompletion">
    /// <c>false</c> for continuous moves: the camera only sends their completion once the motion
    /// ends, so waiting for it would delay the following stop command.
    /// </param>
    public ViscaCommand(string name, IReadOnlyList<byte> payload, TimeSpan? timeout = null, bool waitForCompletion = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A command name is required.", nameof(name));
        }

        if (payload is null || payload.Count == 0)
        {
            throw new ArgumentException("A command payload is required.", nameof(payload));
        }

        Name = name;
        this.payload = new byte[payload.Count];
        for (var i = 0; i < payload.Count; i++)
        {
            this.payload[i] = payload[i];
        }

        Timeout = timeout ?? DefaultTimeout;
        WaitForCompletion = waitForCompletion;
    }

    public string Name { get; }

    /// <summary>How long to wait for the camera's completion message.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>Whether to wait for the completion message, or return once the camera acknowledges.</summary>
    public bool WaitForCompletion { get; }

    public IReadOnlyList<byte> Payload => payload;

    /// <summary>Frames the command as <c>8x payload FF</c> for camera <paramref name="address"/> (1..7).</summary>
    public byte[] ToPacket(int address)
    {
        if (address < 1 || address > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(address), address, "VISCA addresses range from 1 to 7.");
        }

        var packet = new byte[payload.Length + 2];
        packet[0] = (byte)(0x80 | address);
        Array.Copy(payload, 0, packet, 1, payload.Length);
        packet[packet.Length - 1] = Terminator;
        return packet;
    }

    public override string ToString() => Name;
}
