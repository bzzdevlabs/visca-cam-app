using System;
using System.IO.Ports;
using TenveoPtz.Core.Transport;

namespace TenveoPtz.App.Infrastructure.Serial;

/// <summary>Byte transport over a COM port (8 data bits, no parity, 1 stop bit, as VISCA requires).</summary>
internal sealed class SerialPortTransport : IByteTransport
{
    private readonly SerialPort port;

    public SerialPortTransport(string portName, int baudRate)
    {
        port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            WriteTimeout = 1000,
        };
        port.Open();
    }

    public void Write(byte[] data) => port.Write(data, 0, data.Length);

    public int ReadByte(TimeSpan timeout)
    {
        port.ReadTimeout = Math.Max(1, (int)timeout.TotalMilliseconds);
        try
        {
            return port.ReadByte();
        }
        catch (TimeoutException)
        {
            return -1;
        }
    }

    public void DiscardInput() => port.DiscardInBuffer();

    public void Dispose() => port.Dispose();
}
