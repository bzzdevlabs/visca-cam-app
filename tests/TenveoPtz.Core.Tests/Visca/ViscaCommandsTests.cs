using System;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Visca;

namespace TenveoPtz.Core.Tests.Visca;

public sealed class ViscaCommandsTests
{
    [Theory]
    [InlineData(Motion.Negative, Motion.None, 0x01, 0x03)]
    [InlineData(Motion.Positive, Motion.None, 0x02, 0x03)]
    [InlineData(Motion.None, Motion.Positive, 0x03, 0x01)]
    [InlineData(Motion.None, Motion.Negative, 0x03, 0x02)]
    [InlineData(Motion.Negative, Motion.Positive, 0x01, 0x01)]
    [InlineData(Motion.None, Motion.None, 0x03, 0x03)]
    public void PanTiltDrive_EncodesDirections(Motion pan, Motion tilt, byte panByte, byte tiltByte)
    {
        var packet = ViscaCommands.PanTiltDrive(pan, tilt, 0x0C, 0x0A).ToPacket(1);

        Assert.Equal(new byte[] { 0x81, 0x01, 0x06, 0x01, 0x0C, 0x0A, panByte, tiltByte, 0xFF }, packet);
    }

    [Fact]
    public void PanTiltDrive_ClampsSpeedsToProtocolLimits()
    {
        var payload = ViscaCommands.PanTiltDrive(Motion.Positive, Motion.Positive, 99, 0).Payload;

        Assert.Equal(ViscaCommands.MaxPanSpeed, payload[3]);
        Assert.Equal(ViscaCommands.MinTiltSpeed, payload[4]);
    }

    [Theory]
    [InlineData(Motion.Positive, 3, 0x23)]
    [InlineData(Motion.Negative, 7, 0x37)]
    [InlineData(Motion.None, 5, 0x00)]
    public void Zoom_EncodesDirectionAndSpeed(Motion direction, int speed, byte expected)
    {
        Assert.Equal(new byte[] { 0x01, 0x04, 0x07, expected }, ViscaCommands.Zoom(direction, speed).Payload);
    }

    [Fact]
    public void Memory_CommandsTargetSlot()
    {
        Assert.Equal(new byte[] { 0x81, 0x01, 0x04, 0x3F, 0x01, 0x05, 0xFF }, ViscaCommands.MemorySet(5).ToPacket(1));
        Assert.Equal(new byte[] { 0x82, 0x01, 0x04, 0x3F, 0x02, 0x05, 0xFF }, ViscaCommands.MemoryRecall(5).ToPacket(2));
        Assert.Equal(new byte[] { 0x01, 0x04, 0x3F, 0x00, 0x05 }, ViscaCommands.MemoryReset(5).Payload);
    }

    [Fact]
    public void Home_AndAutoFocus_MatchSpecification()
    {
        Assert.Equal(new byte[] { 0x01, 0x06, 0x04 }, ViscaCommands.Home().Payload);
        Assert.Equal(new byte[] { 0x01, 0x04, 0x38, 0x02 }, ViscaCommands.AutoFocus(true).Payload);
        Assert.Equal(new byte[] { 0x01, 0x04, 0x38, 0x03 }, ViscaCommands.AutoFocus(false).Payload);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void ToPacket_RejectsInvalidAddress(int address)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ViscaCommands.Home().ToPacket(address));
    }
}
