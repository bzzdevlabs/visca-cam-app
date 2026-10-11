using System;
using System.IO;
using System.Runtime.InteropServices;
using CoreFoundation;
using Foundation;
using ObjCRuntime;

namespace TenveoPtz.Mac.Infrastructure.Uvc;

/// <summary>
/// UVC class requests to the camera terminal of a USB video device, sent through IOKit as
/// control transfers. Class requests do not change the device state, so the device does not
/// have to be opened: macOS keeps streaming the video while the app moves the camera.
/// </summary>
internal sealed class UvcDevice : IDisposable
{
    public const byte GetCurrent = 0x81;
    public const byte GetMinimum = 0x82;
    public const byte GetMaximum = 0x83;
    public const byte GetResolution = 0x84;
    public const byte GetDefault = 0x87;
    private const byte SetCurrent = 0x01;

    // bmRequestType: class request to an interface, device-to-host (get) or host-to-device (set).
    private const byte ClassGet = 0xA1;
    private const byte ClassSet = 0x21;
    private const byte StandardGet = 0x80;
    private const byte GetDescriptorRequest = 0x06;
    private const ushort ConfigurationDescriptor = 0x0200;

    private IntPtr device;
    private readonly ushort cameraIndex;

    private UvcDevice(IntPtr device, ushort cameraIndex)
    {
        this.device = device;
        this.cameraIndex = cameraIndex;
    }

    /// <summary>
    /// Opens the USB device at <paramref name="locationId"/>; returns <c>null</c> when it is not
    /// found or is not a UVC camera with a camera terminal.
    /// </summary>
    public static UvcDevice? Open(uint locationId)
    {
        var device = FindDevice(locationId);
        if (device == IntPtr.Zero)
        {
            return null;
        }

        var cameraIndex = FindCameraTerminal(device);
        if (cameraIndex is null)
        {
            IOKitNative.Release(device);
            return null;
        }

        return new UvcDevice(device, cameraIndex.Value);
    }

    /// <summary>Sends a get request for <paramref name="selector"/>; returns <c>false</c> when the camera rejects it.</summary>
    public bool TryGet(byte request, byte selector, byte[] data) => Transfer(ClassGet, request, selector, data) == IOKitNative.Success;

    public void Set(byte selector, byte[] data)
    {
        var status = Transfer(ClassSet, SetCurrent, selector, data);
        if (status != IOKitNative.Success)
        {
            throw new IOException($"The camera rejected the command (IOKit error 0x{status:X8}).");
        }
    }

    public void Dispose()
    {
        if (device != IntPtr.Zero)
        {
            IOKitNative.Release(device);
            device = IntPtr.Zero;
        }
    }

    private int Transfer(byte requestType, byte request, byte selector, byte[] data)
    {
        if (device == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(UvcDevice));
        }

        return Request(device, requestType, request, (ushort)(selector << 8), cameraIndex, data, out _);
    }

    private static int Request(IntPtr device, byte requestType, byte request, ushort value, ushort index, byte[] data, out int transferred)
    {
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            var usbRequest = new IOKitNative.UsbRequest
            {
                RequestType = requestType,
                Request = request,
                Value = value,
                Index = index,
                Length = (ushort)data.Length,
                Data = handle.AddrOfPinnedObject(),
            };
            var status = IOKitNative.DeviceRequest(device, ref usbRequest);
            transferred = (int)usbRequest.LengthDone;
            return status;
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>Returns an IOUSBDeviceInterface for the device at <paramref name="locationId"/>, or zero.</summary>
    private static IntPtr FindDevice(uint locationId)
    {
        if (IOKitNative.IOServiceGetMatchingServices(0, IOKitNative.IOServiceMatching("IOUSBHostDevice"), out var iterator) != IOKitNative.Success)
        {
            return IntPtr.Zero;
        }

        try
        {
            uint service;
            while ((service = IOKitNative.IOIteratorNext(iterator)) != 0)
            {
                try
                {
                    if (ReadLocation(service) == locationId)
                    {
                        return CreateDeviceInterface(service);
                    }
                }
                finally
                {
                    IOKitNative.IOObjectRelease(service);
                }
            }

            return IntPtr.Zero;
        }
        finally
        {
            IOKitNative.IOObjectRelease(iterator);
        }
    }

    private static uint? ReadLocation(uint service)
    {
        using var key = new CFString("locationID");
        var value = IOKitNative.IORegistryEntryCreateCFProperty(service, key.Handle, IntPtr.Zero, 0);
        return value == IntPtr.Zero ? null : Runtime.GetNSObject<NSNumber>(value, owns: true)?.UInt32Value;
    }

    private static IntPtr CreateDeviceInterface(uint service)
    {
        var pluginType = IOKitNative.CreateUuid(IOKitNative.UsbDeviceUserClientTypeId);
        var interfaceType = IOKitNative.CreateUuid(IOKitNative.PlugInInterfaceId);
        try
        {
            if (IOKitNative.IOCreatePlugInInterfaceForService(service, pluginType, interfaceType, out var plugin, out _) != IOKitNative.Success || plugin == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            try
            {
                return IOKitNative.QueryInterface(plugin, IOKitNative.UsbDeviceInterfaceId, out var device) == IOKitNative.Success ? device : IntPtr.Zero;
            }
            finally
            {
                IOKitNative.Release(plugin);
            }
        }
        finally
        {
            IOKitNative.CFRelease(pluginType);
            IOKitNative.CFRelease(interfaceType);
        }
    }

    /// <summary>
    /// Reads the configuration descriptor and returns the wIndex addressing the camera terminal:
    /// its terminal ID in the high byte, the video control interface number in the low byte.
    /// </summary>
    private static ushort? FindCameraTerminal(IntPtr device)
    {
        var header = new byte[9];
        if (Request(device, StandardGet, GetDescriptorRequest, ConfigurationDescriptor, 0, header, out var length) != IOKitNative.Success || length < 4)
        {
            return null;
        }

        var descriptor = new byte[header[2] | (header[3] << 8)];
        if (Request(device, StandardGet, GetDescriptorRequest, ConfigurationDescriptor, 0, descriptor, out length) != IOKitNative.Success)
        {
            return null;
        }

        return UvcDescriptors.FindCameraTerminal(descriptor, length);
    }
}
