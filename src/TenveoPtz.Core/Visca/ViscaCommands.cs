using System;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Visca;

/// <summary>Factory for the standard Sony VISCA commands supported by Tenveo cameras.</summary>
public static class ViscaCommands
{
    public const int MinPanSpeed = 0x01;
    public const int MaxPanSpeed = 0x18;
    public const int MinTiltSpeed = 0x01;
    public const int MaxTiltSpeed = 0x14;
    public const int MinZoomSpeed = 0x00;
    public const int MaxZoomSpeed = 0x07;

    private static readonly TimeSpan PresetStoreTimeout = TimeSpan.FromSeconds(2);

    public static ViscaCommand PanTiltDrive(Motion pan, Motion tilt, int panSpeed, int tiltSpeed)
    {
        var panByte = pan switch
        {
            Motion.Negative => (byte)0x01,
            Motion.Positive => (byte)0x02,
            _ => (byte)0x03,
        };
        var tiltByte = tilt switch
        {
            Motion.Positive => (byte)0x01,
            Motion.Negative => (byte)0x02,
            _ => (byte)0x03,
        };

        return new ViscaCommand(
            "Pan/tilt drive",
            new byte[]
            {
                0x01, 0x06, 0x01,
                (byte)Clamp(panSpeed, MinPanSpeed, MaxPanSpeed),
                (byte)Clamp(tiltSpeed, MinTiltSpeed, MaxTiltSpeed),
                panByte, tiltByte,
            });
    }

    public static ViscaCommand Home() => new("Home", new byte[] { 0x01, 0x06, 0x04 });

    public static ViscaCommand Zoom(Motion direction, int speed)
    {
        var p = (byte)Clamp(speed, MinZoomSpeed, MaxZoomSpeed);
        var argument = direction switch
        {
            Motion.Positive => (byte)(0x20 | p),
            Motion.Negative => (byte)(0x30 | p),
            _ => (byte)0x00,
        };
        return new ViscaCommand("Zoom", new byte[] { 0x01, 0x04, 0x07, argument });
    }

    public static ViscaCommand Focus(Motion direction)
    {
        var argument = direction switch
        {
            Motion.Positive => (byte)0x02,
            Motion.Negative => (byte)0x03,
            _ => (byte)0x00,
        };
        return new ViscaCommand("Focus", new byte[] { 0x01, 0x04, 0x08, argument });
    }

    public static ViscaCommand AutoFocus(bool enabled) =>
        new("Auto focus", new byte[] { 0x01, 0x04, 0x38, enabled ? (byte)0x02 : (byte)0x03 });

    public static ViscaCommand MemorySet(int slot) =>
        new("Store preset", new byte[] { 0x01, 0x04, 0x3F, 0x01, SlotByte(slot) }, PresetStoreTimeout);

    public static ViscaCommand MemoryRecall(int slot) =>
        new("Recall preset", new byte[] { 0x01, 0x04, 0x3F, 0x02, SlotByte(slot) });

    public static ViscaCommand MemoryReset(int slot) =>
        new("Reset preset", new byte[] { 0x01, 0x04, 0x3F, 0x00, SlotByte(slot) });

    private static byte SlotByte(int slot)
    {
        if (slot < 0 || slot > 0xFE)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "VISCA preset slots range from 0 to 254.");
        }

        return (byte)slot;
    }

    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
}
