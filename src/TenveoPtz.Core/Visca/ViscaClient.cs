using System;
using System.Collections.Generic;
using System.Diagnostics;
using TenveoPtz.Core.Transport;

namespace TenveoPtz.Core.Visca;

/// <summary>
/// Sends <see cref="ViscaCommand"/>s over a byte transport and interprets the camera's replies.
/// Not thread-safe: callers serialise access (see <see cref="Execution.ICommandExecutor"/>).
/// </summary>
public sealed class ViscaClient : IDisposable
{
    private const int MaxMessageLength = 16;

    private readonly IByteTransport transport;

    /// <param name="transport">Transport to use. Ownership is transferred to the client.</param>
    /// <param name="address">Camera address, 1..7 (1 for a single camera).</param>
    public ViscaClient(IByteTransport transport, int address)
    {
        if (address < 1 || address > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(address), address, "VISCA addresses range from 1 to 7.");
        }

        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Address = address;
    }

    public int Address { get; }

    /// <summary>
    /// Sends <paramref name="command"/> and waits for its completion (or only its acknowledgement,
    /// see <see cref="ViscaCommand.WaitForCompletion"/>).
    /// Returns <c>false</c> when the camera does not confirm in time (some models never reply).
    /// </summary>
    /// <exception cref="ViscaException">The camera rejected the command.</exception>
    public bool Execute(ViscaCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        transport.DiscardInput();
        transport.Write(command.ToPacket(Address));

        var clock = Stopwatch.StartNew();
        while (true)
        {
            var message = ReadMessage(command.Timeout - clock.Elapsed);
            if (message is null)
            {
                return false;
            }

            if (message.Count < 3)
            {
                continue;
            }

            switch (message[1] & 0xF0)
            {
                case 0x40: // ACK: command accepted, completion follows.
                    if (command.WaitForCompletion)
                    {
                        continue;
                    }

                    return true;
                case 0x50: // Completion.
                    return true;
                case 0x60:
                    throw new ViscaException(command.Name, message[2]);
            }
        }
    }

    public void Dispose() => transport.Dispose();

    private List<byte>? ReadMessage(TimeSpan remaining)
    {
        var clock = Stopwatch.StartNew();
        var message = new List<byte>(MaxMessageLength);
        while (clock.Elapsed < remaining)
        {
            var value = transport.ReadByte(remaining - clock.Elapsed);
            if (value < 0)
            {
                return null;
            }

            message.Add((byte)value);
            if (value == ViscaCommand.Terminator)
            {
                return message;
            }

            if (message.Count >= MaxMessageLength)
            {
                message.Clear(); // Garbage on the line: resynchronise on the next terminator.
            }
        }

        return null;
    }
}
