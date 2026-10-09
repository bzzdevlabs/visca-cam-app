using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using TenveoPtz.Core.Transport;

namespace TenveoPtz.App.Infrastructure.Serial;

internal sealed class SerialPortProvider : ISerialPortProvider
{
    public IReadOnlyList<string> GetPortNames() =>
        SerialPort.GetPortNames()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(PortNumber)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IByteTransport Open(string portName, int baudRate) => new SerialPortTransport(portName, baudRate);

    /// <summary>Sorts COM2 before COM10.</summary>
    private static int PortNumber(string name) =>
        int.TryParse(new string(name.Where(char.IsDigit).ToArray()), out var number) ? number : int.MaxValue;
}
