using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Tests.Fakes;
using TenveoPtz.Core.Visca;

namespace TenveoPtz.Core.Tests.Visca;

public sealed class ViscaClientTests
{
    private readonly FakeTransport transport = new();

    [Fact]
    public void Execute_WritesFramedPacket()
    {
        var client = new ViscaClient(transport, 1);

        client.Execute(ViscaCommands.Home());

        Assert.Equal(new byte[] { 0x81, 0x01, 0x06, 0x04, 0xFF }, Assert.Single(transport.Written));
    }

    [Fact]
    public void Execute_ReturnsTrueAfterAckAndCompletion()
    {
        transport.QueueReply(0x90, 0x41, 0xFF, 0x90, 0x51, 0xFF);
        var client = new ViscaClient(transport, 1);

        Assert.True(client.Execute(ViscaCommands.Home()));
    }

    [Fact]
    public void Execute_ContinuousMoveReturnsOnAcknowledge()
    {
        transport.QueueReply(0x90, 0x41, 0xFF);
        var client = new ViscaClient(transport, 1);

        Assert.True(client.Execute(ViscaCommands.PanTiltDrive(Motion.Positive, Motion.None, 1, 1)));
    }

    [Fact]
    public void Execute_ReturnsFalseWhenCameraStaysSilent()
    {
        var client = new ViscaClient(transport, 1);

        Assert.False(client.Execute(ViscaCommands.Home()));
    }

    [Fact]
    public void Execute_ThrowsOnErrorReply()
    {
        transport.QueueReply(0x90, 0x60, 0x02, 0xFF);
        var client = new ViscaClient(transport, 1);

        var error = Assert.Throws<ViscaException>(() => client.Execute(ViscaCommands.Home()));
        Assert.Equal(0x02, error.ErrorCode);
        Assert.Contains("syntax error", error.Message);
    }

    [Fact]
    public void Dispose_ReleasesTransport()
    {
        new ViscaClient(transport, 1).Dispose();

        Assert.True(transport.Disposed);
    }
}
