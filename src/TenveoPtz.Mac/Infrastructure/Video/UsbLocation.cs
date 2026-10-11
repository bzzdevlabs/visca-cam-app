using System;
using System.Globalization;

namespace TenveoPtz.Mac.Infrastructure.Video;

/// <summary>
/// Finds the USB location of a camera from its AVFoundation unique ID. For USB (UVC) cameras
/// macOS builds that ID as <c>0x</c> + location ID + vendor ID (4 digits) + product ID (4 digits),
/// e.g. <c>0x134000046d085c</c>; built-in and network cameras use other formats.
/// </summary>
internal static class UsbLocation
{
    public static bool TryParse(string? uniqueId, out uint locationId)
    {
        locationId = 0;
        if (uniqueId is null || !uniqueId.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || uniqueId.Length <= 10)
        {
            return false;
        }

        var location = uniqueId.Substring(2, uniqueId.Length - 10);
        return location.Length <= 8 && uint.TryParse(location, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out locationId);
    }
}
