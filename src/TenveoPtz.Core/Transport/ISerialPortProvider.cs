using System.Collections.Generic;

namespace TenveoPtz.Core.Transport;

/// <summary>Discovers and opens serial ports.</summary>
public interface ISerialPortProvider
{
    IReadOnlyList<string> GetPortNames();

    IByteTransport Open(string portName, int baudRate);
}
