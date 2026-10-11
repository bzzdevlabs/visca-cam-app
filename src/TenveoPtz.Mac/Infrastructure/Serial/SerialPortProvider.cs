using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using TenveoPtz.App.Infrastructure.Serial;
using TenveoPtz.Core.Transport;

namespace TenveoPtz.Mac.Infrastructure.Serial;

/// <summary>
/// Serial ports as <c>/dev/cu.*</c> devices: the "call-up" side, which opens without waiting for
/// a carrier signal (the <c>/dev/tty.*</c> twin would block on most USB-to-serial adapters).
/// </summary>
internal sealed class SerialPortProvider : ISerialPortProvider
{
    // Always present on a Mac, never a camera.
    private static readonly string[] SystemPorts = { "/dev/cu.Bluetooth-Incoming-Port", "/dev/cu.debug-console" };

    public IReadOnlyList<string> GetPortNames() =>
        SerialPort.GetPortNames()
            .Where(name => name.StartsWith("/dev/cu.", StringComparison.Ordinal) && !SystemPorts.Contains(name))
            .Distinct()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IByteTransport Open(string portName, int baudRate) => new SerialPortTransport(portName, baudRate);
}
