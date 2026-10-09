using System.IO;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Tests.Fakes;
using TenveoPtz.Core.Visca;

namespace TenveoPtz.Core.Tests.Ptz;

public sealed class ViscaPtzControllerTests
{
    private readonly FakeTransport transport = new();
    private readonly InlineExecutor executor = new();
    private readonly ViscaPtzController controller;

    public ViscaPtzControllerTests()
    {
        controller = new ViscaPtzController(new ViscaClient(transport, 1), executor, "VISCA on COM3");
    }

    [Fact]
    public void Move_AtFullSpeedUsesMaximumProtocolSpeeds()
    {
        controller.Move(Motion.Positive, Motion.None, 1.0);

        var packet = Assert.Single(transport.Written);
        Assert.Equal(ViscaCommands.MaxPanSpeed, packet[4]);
        Assert.Equal(ViscaCommands.MaxTiltSpeed, packet[5]);
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
