using System.Linq;
using TenveoPtz.Core.Diagnostics;
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
    public void Send_ReturnsTheInquiryReply()
    {
        transport.QueueReply(0x90, 0x50, 0x02, 0xFF);
        var client = new ViscaClient(transport, 1);

        var reply = client.Send(ViscaCommands.Raw(new byte[] { 0x09, 0x04, 0x47 }));

        Assert.Equal(new byte[] { 0x90, 0x50, 0x02, 0xFF }, reply);
    }

    [Fact]
    public void Send_LogsTrafficAtDebugLevel()
    {
        transport.QueueReply(0x90, 0x41, 0xFF, 0x90, 0x51, 0xFF);
        var hub = new LogHub(LogLevel.Debug, 10);
        var client = new ViscaClient(transport, 1, new Logger(hub, "VISCA"));

        client.Execute(ViscaCommands.Home());

        Assert.Equal(
            new[] { "TX 81 01 06 04 FF (Home)", "RX 90 41 FF", "RX 90 51 FF" },
            hub.Recent().Select(e => e.Message));
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

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Constructor_RejectsInvalidAddress(int address)
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ViscaClient(transport, address));
    }

    [Fact]
    public void Send_RequiresACommand()
    {
        Assert.Throws<System.ArgumentNullException>(() => new ViscaClient(transport, 1).Send(null!));
    }

    [Fact]
    public void Send_SkipsGarbageBeforeTheReply()
    {
        // A long run of bytes without terminator is line noise: the reply after it is still read.
        transport.QueueReply(Enumerable.Repeat((byte)0x55, 40).ToArray());
        transport.QueueReply(0x90, 0x41, 0xFF, 0x90, 0x51, 0xFF);

        Assert.True(new ViscaClient(transport, 1).Execute(ViscaCommands.Home()));
    }

    [Theory]
    [InlineData(0x01, "message length error")]
    [InlineData(0x03, "command buffer full")]
    [InlineData(0x04, "command cancelled")]
    [InlineData(0x05, "no socket")]
    [InlineData(0x41, "command not executable")]
    [InlineData(0x7F, "camera error")]
    public void Errors_AreDescribed(byte code, string description)
    {
        var error = new ViscaException("Home", code);

        Assert.Equal($"Home: {description} (VISCA error 0x{code:X2})", error.Message);
        Assert.Equal("Home", error.CommandName);
    }
}
