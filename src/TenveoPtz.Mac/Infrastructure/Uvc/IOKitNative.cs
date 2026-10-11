using System;
using System.Runtime.InteropServices;

namespace TenveoPtz.Mac.Infrastructure.Uvc;

/// <summary>
/// IOKit and CoreFoundation functions used to talk to a USB device from user space. The USB
/// "plug-in" interfaces are C structures of function pointers (COM style); their slot numbers
/// below come from IOUSBLib.h (they have been stable since macOS 10.0).
/// </summary>
internal static unsafe class IOKitNative
{
    public const int Success = 0;

    private const string IOKitLibrary = "/System/Library/Frameworks/IOKit.framework/IOKit";
    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    // Slots in IOCFPlugInInterface, IOUSBDeviceInterface (IUnknown first, then the USB functions).
    private const int QueryInterfaceSlot = 1;
    private const int ReleaseSlot = 3;
    private const int DeviceRequestSlot = 26;

    public static readonly byte[] PlugInInterfaceId = Bytes(0xC2, 0x44, 0xE8, 0x58, 0x10, 0x9C, 0x11, 0xD4, 0x91, 0xD4, 0x00, 0x50, 0xE4, 0xC6, 0x42, 0x6F);
    public static readonly byte[] UsbDeviceUserClientTypeId = Bytes(0x9D, 0xC7, 0xB7, 0x80, 0x9E, 0xC0, 0x11, 0xD4, 0xA5, 0x4F, 0x00, 0x0A, 0x27, 0x05, 0x28, 0x61);
    public static readonly byte[] UsbDeviceInterfaceId = Bytes(0x5C, 0x81, 0x87, 0xD0, 0x9E, 0xF3, 0x11, 0xD4, 0x8B, 0x45, 0x00, 0x0A, 0x27, 0x05, 0x28, 0x61);

    [DllImport(IOKitLibrary)]
    public static extern IntPtr IOServiceMatching([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    /// <summary>Consumes <paramref name="matching"/>.</summary>
    [DllImport(IOKitLibrary)]
    public static extern int IOServiceGetMatchingServices(uint mainPort, IntPtr matching, out uint iterator);

    [DllImport(IOKitLibrary)]
    public static extern uint IOIteratorNext(uint iterator);

    [DllImport(IOKitLibrary)]
    public static extern int IOObjectRelease(uint ioObject);

    [DllImport(IOKitLibrary)]
    public static extern IntPtr IORegistryEntryCreateCFProperty(uint entry, IntPtr key, IntPtr allocator, uint options);

    [DllImport(IOKitLibrary)]
    public static extern int IOCreatePlugInInterfaceForService(uint service, IntPtr pluginType, IntPtr interfaceType, out IntPtr plugin, out int score);

    [DllImport(CoreFoundationLibrary)]
    public static extern IntPtr CFUUIDCreateFromUUIDBytes(IntPtr allocator, UuidBytes bytes);

    [DllImport(CoreFoundationLibrary)]
    public static extern void CFRelease(IntPtr value);

    /// <summary>Calls <c>QueryInterface</c> of a plug-in interface.</summary>
    public static int QueryInterface(IntPtr instance, byte[] interfaceId, out IntPtr result)
    {
        var function = (delegate* unmanaged<IntPtr, UuidBytes, IntPtr*, int>)Slot(instance, QueryInterfaceSlot);
        IntPtr value;
        var status = function(instance, UuidBytes.From(interfaceId), &value);
        result = value;
        return status;
    }

    /// <summary>Calls <c>Release</c> of a plug-in or device interface.</summary>
    public static void Release(IntPtr instance) =>
        ((delegate* unmanaged<IntPtr, uint>)Slot(instance, ReleaseSlot))(instance);

    /// <summary>Calls <c>DeviceRequest</c> of an IOUSBDeviceInterface: a control transfer on the default pipe.</summary>
    public static int DeviceRequest(IntPtr device, ref UsbRequest request)
    {
        fixed (UsbRequest* pointer = &request)
        {
            return ((delegate* unmanaged<IntPtr, UsbRequest*, int>)Slot(device, DeviceRequestSlot))(device, pointer);
        }
    }

    /// <summary>Creates a CFUUID (to release with <see cref="CFRelease"/>).</summary>
    public static IntPtr CreateUuid(byte[] bytes) => CFUUIDCreateFromUUIDBytes(IntPtr.Zero, UuidBytes.From(bytes));

    /// <summary>An interface pointer points to a pointer to its function table.</summary>
    private static IntPtr Slot(IntPtr instance, int slot) => (*(IntPtr**)instance)[slot];

    private static byte[] Bytes(params byte[] bytes) => bytes;

    /// <summary>CFUUIDBytes: 16 bytes passed by value.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UuidBytes
    {
        public ulong First;
        public ulong Second;

        public static UuidBytes From(byte[] bytes) => MemoryMarshal.Read<UuidBytes>(bytes);
    }

    /// <summary>IOUSBDevRequest.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UsbRequest
    {
        public byte RequestType;
        public byte Request;
        public ushort Value;
        public ushort Index;
        public ushort Length;
        public IntPtr Data;
        public uint LengthDone;
    }
}
