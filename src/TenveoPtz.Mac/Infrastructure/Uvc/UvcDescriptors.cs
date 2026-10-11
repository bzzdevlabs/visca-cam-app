namespace TenveoPtz.Mac.Infrastructure.Uvc;

/// <summary>Parsing of the USB configuration descriptor of a UVC camera.</summary>
internal static class UvcDescriptors
{
    private const byte InterfaceDescriptor = 0x04;
    private const byte ClassSpecificInterface = 0x24;
    private const byte VideoClass = 0x0E;
    private const byte VideoControlSubclass = 0x01;
    private const byte InputTerminal = 0x02;
    private const int CameraTerminalType = 0x0201;

    /// <summary>
    /// Returns the wIndex of UVC requests to the camera terminal (terminal ID in the high byte,
    /// video control interface number in the low byte), or <c>null</c> when there is none.
    /// </summary>
    public static ushort? FindCameraTerminal(byte[] descriptor, int length)
    {
        int? videoControlInterface = null;
        for (var offset = 0; offset + 2 <= length && descriptor[offset] >= 2; offset += descriptor[offset])
        {
            var size = descriptor[offset];
            var type = descriptor[offset + 1];
            if (offset + size > length)
            {
                break;
            }

            if (type == InterfaceDescriptor && size >= 7)
            {
                var isVideoControl = descriptor[offset + 5] == VideoClass && descriptor[offset + 6] == VideoControlSubclass;
                videoControlInterface = isVideoControl ? descriptor[offset + 2] : null;
            }
            else if (type == ClassSpecificInterface && videoControlInterface is not null && size >= 6
                && descriptor[offset + 2] == InputTerminal
                && (descriptor[offset + 4] | (descriptor[offset + 5] << 8)) == CameraTerminalType)
            {
                return (ushort)((descriptor[offset + 3] << 8) | videoControlInterface.Value);
            }
        }

        return null;
    }
}
