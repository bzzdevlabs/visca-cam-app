using System.IO;
using System.Linq;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Tests.Fakes;
using TenveoPtz.Core.Visca;

namespace TenveoPtz.Core.Tests.Ptz;

public sealed class ViscaPtzControllerTests
{
    private readonly FakeTransport transport = new();
    private readonly InlineExecutor executor = new();
    private readonly LogHub log = new(LogLevel.Info, 10);
    private readonly ViscaPtzController controller;

    public ViscaPtzControllerTests()
    {
        controller = new ViscaPtzController(new ViscaClient(transport, 1), executor, "VISCA on COM3", new Logger(log, "VISCA"));
    }

    [Fact]
    public void SendRaw_WritesPacketAndLogsReply()
    {
        transport.QueueReply(0x90, 0x50, 0x02, 0xFF);

        controller.SendRaw(new byte[] { 0x09, 0x04, 0x47 });

        Assert.Equal(new byte[] { 0x81, 0x09, 0x04, 0x47, 0xFF }, Assert.Single(transport.Written));
        Assert.Equal("Raw command 09 04 47: reply 90 50 02 FF", log.Recent().Single().Message);
    }

    [Fact]
    public void Move_AtFullSpeedStaysWellBelowProtocolMaximum()
    {
        controller.Move(Motion.Positive, Motion.None, 1.0);

        var packet = Assert.Single(transport.Written);
        Assert.Equal(ViscaPtzController.MaxDrivePanSpeed, packet[4]);
        Assert.Equal(ViscaPtzController.MaxDriveTiltSpeed, packet[5]);
        Assert.True(packet[4] < ViscaCommands.MaxPanSpeed);
        Assert.True(packet[5] < ViscaCommands.MaxTiltSpeed);
    }

    [Fact]
    public void Move_EachSliderStepGivesADistinctPanSpeed()
    {
        var speeds = Enumerable.Range(0, 10).Select(step =>
        {
            transport.Written.Clear();
            controller.Move(Motion.Positive, Motion.None, step / 9.0);
            return transport.Written.Single()[4];
        }).ToList();

        Assert.Equal(ViscaCommands.MinPanSpeed, speeds[0]);
        Assert.Equal(speeds.Count, speeds.Distinct().Count());
    }

    [Fact]
    public void Presets_UseCameraMemorySlot()
    {
        var preset = new Preset { Name = "Pulpit", Slot = 7 };

        controller.StorePreset(preset);
        controller.RecallPreset(preset);

        Assert.Equal(ViscaCommands.MemorySet(7).ToPacket(1), transport.Written[0]);
        Assert.Equal(ViscaCommands.MemoryRecall(7).ToPacket(1), transport.Written[1]);
    }

    [Fact]
    public void CameraErrors_AreReportedThroughFaulted()
    {
        ErrorEventArgs? fault = null;
        controller.Faulted += (_, e) => fault = e;
        transport.QueueReply(0x90, 0x61, 0x41, 0xFF);

        controller.Home();

        Assert.IsType<ViscaException>(fault?.GetException());
    }

    [Fact]
    public void Dispose_ReleasesExecutorAndTransport()
    {
        controller.Dispose();

        Assert.True(executor.Disposed);
        Assert.True(transport.Disposed);
    }
}
